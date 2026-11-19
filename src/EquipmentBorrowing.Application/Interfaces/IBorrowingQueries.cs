using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public enum BorrowingStatusFilter
{
    All,
    Active,
    Returned
}

public enum BorrowingSortField
{
    CheckoutDate,
    ExpectedReturnDate
}

public sealed record BorrowingSearchCriteria(
    string? Text = null,
    BorrowingStatusFilter Status = BorrowingStatusFilter.All,
    BorrowingSortField SortField = BorrowingSortField.CheckoutDate,
    bool Descending = true);

public interface IBorrowingQueries
{
    Task<IReadOnlyList<Borrowing>> GetByStudentAsync(int studentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Borrowing>> GetByEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Borrowing>> SearchAsync(BorrowingSearchCriteria criteria, CancellationToken cancellationToken = default);
}
