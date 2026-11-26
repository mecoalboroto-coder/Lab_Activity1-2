using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfBorrowingTransactions : IBorrowingTransactions
{
    private const string UnknownOutcomeMessage =
        "The connection was lost while saving, so the result is unknown. " +
        "The data was not retried. Refresh and check the current records before trying again.";

    private const string FailedMessage =
        "The database operation failed and no changes were saved. Please try again.";

    private const string ConflictMessage =
        "Another change conflicted with this request, so nothing was saved. The data has been refreshed.";

    private readonly IDbContextFactory<EquipmentBorrowingDbContext> _dbContextFactory;
    private readonly IFailureInjector _failureInjector;

    public EfBorrowingTransactions(IDbContextFactory<EquipmentBorrowingDbContext> dbContextFactory, IFailureInjector failureInjector)
    {
        _dbContextFactory = dbContextFactory;
        _failureInjector = failureInjector;
    }

    public async Task<BorrowResult> CheckoutAsync(
        int studentId,
        IReadOnlyList<int> equipmentIds,
        DateOnly checkoutDate,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var committing = false;

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var student = (await context.Students
                .FromSqlInterpolated($"SELECT * FROM \"Students\" WHERE \"Id\" = {studentId} FOR UPDATE")
                .ToListAsync(cancellationToken)).FirstOrDefault();

            var ids = equipmentIds.Distinct().OrderBy(id => id).ToArray();
            var equipment = await context.Equipment
                .FromSqlInterpolated(
                    $"SELECT * FROM \"Equipment\" WHERE \"Id\" = ANY({ids}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);

            var activeCount = await context.Borrowings.CountAsync(
                b => b.StudentId == studentId && b.Status == BorrowingStatus.Active,
                cancellationToken);

            var violation = CheckoutPolicy.Validate(
                student, equipmentIds, equipment, activeCount, checkoutDate, expectedReturnDate);

            if (violation is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return BorrowResult.Failed(violation.Message, violation.Kind);
            }

            var created = new List<Borrowing>();
            foreach (var item in equipment)
            {
                item.MarkUnavailable();
                var borrowing = new Borrowing(studentId, item.Id, checkoutDate, expectedReturnDate);
                context.Borrowings.Add(borrowing);
                created.Add(borrowing);
            }

            await context.SaveChangesAsync(cancellationToken);
            _failureInjector.BeforeCommit();

            committing = true;
            await transaction.CommitAsync(cancellationToken);

            return BorrowResult.Succeeded(created);
        }
        catch (DbUpdateException ex) when (!committing
            && ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return BorrowResult.Failed(ConflictMessage, FailureKind.Conflict);
        }
        catch (Exception ex) when (ex is NpgsqlException or DbUpdateException or IOException
            or TimeoutException or InvalidOperationException)
        {
            return committing
                ? BorrowResult.Unknown(UnknownOutcomeMessage)
                : BorrowResult.Failed(FailedMessage, FailureKind.Server);
        }
    }

    public async Task<ReturnResult> ReturnAsync(
        int borrowingId,
        DateOnly returnDate,
        CancellationToken cancellationToken = default)
    {
        var committing = false;

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var borrowing = (await context.Borrowings
                .FromSqlInterpolated($"SELECT * FROM \"Borrowings\" WHERE \"Id\" = {borrowingId} FOR UPDATE")
                .ToListAsync(cancellationToken)).FirstOrDefault();

            if (borrowing is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ReturnResult.Failed($"Borrowing {borrowingId} was not found. The list has been refreshed.", FailureKind.NotFound);
            }

            if (borrowing.Status == BorrowingStatus.Returned)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ReturnResult.Failed("This borrowing has already been returned.", FailureKind.Conflict);
            }

            if (returnDate < borrowing.DateBorrowed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ReturnResult.Failed("The return date cannot precede the checkout date.", FailureKind.Conflict);
            }

            var equipmentId = borrowing.EquipmentId;
            var equipment = (await context.Equipment
                .FromSqlInterpolated($"SELECT * FROM \"Equipment\" WHERE \"Id\" = {equipmentId} FOR UPDATE")
                .ToListAsync(cancellationToken)).FirstOrDefault();

            if (equipment is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ReturnResult.Failed("The borrowed equipment record could not be found.", FailureKind.NotFound);
            }

            await context.Entry(borrowing).Reference(b => b.Student).LoadAsync(cancellationToken);
            borrowing.MarkReturned(returnDate);
            equipment.MarkAvailable();

            await context.SaveChangesAsync(cancellationToken);

            committing = true;
            await transaction.CommitAsync(cancellationToken);

            return ReturnResult.Succeeded(borrowing);
        }
        catch (Exception ex) when (ex is NpgsqlException or DbUpdateException or IOException
            or TimeoutException or InvalidOperationException)
        {
            return committing
                ? ReturnResult.Unknown(UnknownOutcomeMessage)
                : ReturnResult.Failed(FailedMessage, FailureKind.Server);
        }
    }
}
