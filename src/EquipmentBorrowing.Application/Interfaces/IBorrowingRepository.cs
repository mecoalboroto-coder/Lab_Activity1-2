using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

/// <summary>
/// Abstraction over borrowing-record storage. Every method here exists
/// because BorrowEquipmentService (and, eventually, ReturnEquipmentService)
/// currently needs it — not because it might be useful someday.
/// </summary>
public interface IBorrowingRepository
{
    Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of currently Active borrowings held by a student,
    /// used to enforce the maximum-active-borrowings rule.
    /// </summary>
    Task<int> CountActiveByStudentAsync(int studentId, CancellationToken cancellationToken = default);

    Task<Borrowing?> GetActiveByStudentAndEquipmentAsync(
        int studentId,
        int equipmentId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default);
}
