using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public class MultiCheckoutService
{
    private readonly IBorrowingTransactions _transactions;

    public MultiCheckoutService(IBorrowingTransactions transactions)
    {
        _transactions = transactions;
    }

    public Task<BorrowResult> CheckoutAsync(
        int studentId,
        IReadOnlyList<int> equipmentIds,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        if (equipmentIds.Count < 2)
            return Task.FromResult(BorrowResult.Failed("A multi-item checkout needs at least two items."));

        var today = DateOnly.FromDateTime(DateTime.Now);
        return _transactions.CheckoutAsync(studentId, equipmentIds, today, expectedReturnDate, cancellationToken);
    }
}
