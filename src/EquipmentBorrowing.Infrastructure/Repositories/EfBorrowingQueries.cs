using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfBorrowingQueries : IBorrowingQueries
{
    private readonly IDbContextFactory<EquipmentBorrowingDbContext> _dbContextFactory;

    public EfBorrowingQueries(IDbContextFactory<EquipmentBorrowingDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<Borrowing>> GetByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .Where(b => b.StudentId == studentId)
            .OrderByDescending(b => b.DateBorrowed)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Borrowing>> GetByEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .Where(b => b.EquipmentId == equipmentId)
            .OrderByDescending(b => b.DateBorrowed)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Borrowing>> SearchAsync(BorrowingSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .AsQueryable();

        var text = criteria.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            var pattern = "%" + text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

            query = query.Where(b =>
                EF.Functions.ILike(b.Student!.Name, pattern)
                || EF.Functions.ILike(b.Student!.StudentNumber, pattern)
                || EF.Functions.ILike(b.Equipment!.Name, pattern));
        }

        query = criteria.Status switch
        {
            BorrowingStatusFilter.Active => query.Where(b => b.Status == BorrowingStatus.Active),
            BorrowingStatusFilter.Returned => query.Where(b => b.Status == BorrowingStatus.Returned),
            _ => query
        };

        IOrderedQueryable<Borrowing> ordered = (criteria.SortField, criteria.Descending) switch
        {
            (BorrowingSortField.CheckoutDate, false) => query.OrderBy(b => b.DateBorrowed),
            (BorrowingSortField.CheckoutDate, true) => query.OrderByDescending(b => b.DateBorrowed),
            (BorrowingSortField.ExpectedReturnDate, false) => query.OrderBy(b => b.ExpectedReturnDate),
            _ => query.OrderByDescending(b => b.ExpectedReturnDate)
        };

        return await ordered.ThenBy(b => b.Id).ToListAsync(cancellationToken);
    }
}
