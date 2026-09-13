using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IBorrowingRepository
{
    Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default);

    Task<int> CountActiveByStudentAsync(int studentId, CancellationToken cancellationToken = default);

    Task<Borrowing?> GetActiveByStudentAndEquipmentAsync(
        int studentId,
        int equipmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Borrowing>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default);
}