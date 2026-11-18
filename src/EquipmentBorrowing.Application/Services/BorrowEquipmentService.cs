using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public class BorrowEquipmentService
{
    private readonly IBorrowingTransactions _transactions;

    public BorrowEquipmentService(IBorrowingTransactions transactions)
    {
        _transactions = transactions;
    }

    public Task<BorrowResult> BorrowAsync(
        int studentId,
        int equipmentId,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return _transactions.CheckoutAsync(
            studentId,
            new[] { equipmentId },
            today,
            expectedReturnDate,
            cancellationToken);
    }
}
