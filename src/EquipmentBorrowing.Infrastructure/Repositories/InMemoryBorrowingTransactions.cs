using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingTransactions : IBorrowingTransactions
{
    private readonly IStudentRepository _students;
    private readonly IEquipmentRepository _equipment;
    private readonly IBorrowingRepository _borrowings;

    public InMemoryBorrowingTransactions(
        IStudentRepository students,
        IEquipmentRepository equipment,
        IBorrowingRepository borrowings)
    {
        _students = students;
        _equipment = equipment;
        _borrowings = borrowings;
    }

    public async Task<BorrowResult> CheckoutAsync(
        int studentId,
        IReadOnlyList<int> equipmentIds,
        DateOnly checkoutDate,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var student = await _students.GetByIdAsync(studentId, cancellationToken);

        var found = new List<Equipment>();
        foreach (var id in equipmentIds.Distinct())
        {
            var item = await _equipment.GetByIdAsync(id, cancellationToken);
            if (item is not null)
                found.Add(item);
        }

        var activeCount = await _borrowings.CountActiveByStudentAsync(studentId, cancellationToken);

        var violation = CheckoutPolicy.Validate(
            student, equipmentIds, found, activeCount, checkoutDate, expectedReturnDate);

        if (violation is not null)
            return BorrowResult.Failed(violation.Message, violation.Kind);

        var created = new List<Borrowing>();
        foreach (var item in found)
        {
            item.MarkUnavailable();
            await _equipment.UpdateAsync(item, cancellationToken);

            var borrowing = new Borrowing(studentId, item.Id, checkoutDate, expectedReturnDate);
            await _borrowings.AddAsync(borrowing, cancellationToken);
            created.Add(borrowing);
        }

        return BorrowResult.Succeeded(created);
    }

    public async Task<ReturnResult> ReturnAsync(
        int borrowingId,
        DateOnly returnDate,
        CancellationToken cancellationToken = default)
    {
        var borrowing = await _borrowings.GetByIdAsync(borrowingId, cancellationToken);

        if (borrowing is null)
            return ReturnResult.Failed($"Borrowing {borrowingId} was not found.", FailureKind.NotFound);

        if (borrowing.Status == BorrowingStatus.Returned)
            return ReturnResult.Failed("This borrowing has already been returned.", FailureKind.Conflict);

        if (returnDate < borrowing.DateBorrowed)
            return ReturnResult.Failed("The return date cannot precede the checkout date.", FailureKind.Conflict);

        borrowing.MarkReturned(returnDate);
        await _borrowings.UpdateAsync(borrowing, cancellationToken);

        var equipment = await _equipment.GetByIdAsync(borrowing.EquipmentId, cancellationToken);
        if (equipment is not null)
        {
            equipment.MarkAvailable();
            await _equipment.UpdateAsync(equipment, cancellationToken);
        }

        return ReturnResult.Succeeded(borrowing);
    }
}
