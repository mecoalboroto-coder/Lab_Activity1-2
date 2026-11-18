using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public class ReturnEquipmentService
{
    private readonly IBorrowingTransactions _transactions;

    public ReturnEquipmentService(IBorrowingTransactions transactions)
    {
        _transactions = transactions;
    }

    public Task<ReturnResult> ReturnAsync(
        int borrowingId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return _transactions.ReturnAsync(borrowingId, today, cancellationToken);
    }
}
