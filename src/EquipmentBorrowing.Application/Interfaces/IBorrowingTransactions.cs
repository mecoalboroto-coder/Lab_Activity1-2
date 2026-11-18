using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IBorrowingTransactions
{
    Task<BorrowResult> CheckoutAsync(
        int studentId,
        IReadOnlyList<int> equipmentIds,
        DateOnly checkoutDate,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default);

    Task<ReturnResult> ReturnAsync(
        int borrowingId,
        DateOnly returnDate,
        CancellationToken cancellationToken = default);
}