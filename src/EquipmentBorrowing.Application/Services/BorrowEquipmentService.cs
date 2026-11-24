using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public class BorrowEquipmentService
{
    private readonly IBorrowingTransactions _transactions;
    private readonly IClock _clock;

    public BorrowEquipmentService(IBorrowingTransactions transactions, IClock? clock = null)
    {
        _transactions = transactions;
        _clock = clock ?? new SystemClock();
    }

    public Task<BorrowResult> BorrowAsync(
        int studentId,
        int equipmentId,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        return _transactions.CheckoutAsync(
            studentId,
            new[] { equipmentId },
            _clock.Today,
            expectedReturnDate,
            cancellationToken);
    }
}
