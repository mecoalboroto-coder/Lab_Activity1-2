using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public class ReturnEquipmentService
{
    private readonly IBorrowingTransactions _transactions;
    private readonly IClock _clock;

    public ReturnEquipmentService(IBorrowingTransactions transactions, IClock? clock = null)
    {
        _transactions = transactions;
        _clock = clock ?? new SystemClock();
    }

    public Task<ReturnResult> ReturnAsync(
        int borrowingId,
        CancellationToken cancellationToken = default)
    {
        return _transactions.ReturnAsync(borrowingId, _clock.Today, cancellationToken);
    }
}
