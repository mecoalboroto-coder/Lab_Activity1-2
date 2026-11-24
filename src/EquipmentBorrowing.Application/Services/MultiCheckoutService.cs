using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public class MultiCheckoutService
{
    private readonly IBorrowingTransactions _transactions;
    private readonly IClock _clock;

    public MultiCheckoutService(IBorrowingTransactions transactions, IClock? clock = null)
    {
        _transactions = transactions;
        _clock = clock ?? new SystemClock();
    }

    public Task<BorrowResult> CheckoutAsync(
        int studentId,
        IReadOnlyList<int> equipmentIds,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        if (equipmentIds.Count < 2)
            return Task.FromResult(BorrowResult.Failed("A multi-item checkout needs at least two items.", FailureKind.Invalid));

        return _transactions.CheckoutAsync(studentId, equipmentIds, _clock.Today, expectedReturnDate, cancellationToken);
    }
}
