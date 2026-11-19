using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> NumberExistsAsync(string normalizedNumber, int? excludingId, CancellationToken cancellationToken = default);

    Task AddAsync(Student student, CancellationToken cancellationToken = default);

    Task<bool> UpdateDetailsAsync(
        int id,
        string studentNumber,
        string name,
        string? contact,
        CancellationToken cancellationToken = default);

    Task<EligibilityChange> SetEligibilityAsync(int id, bool allowed, CancellationToken cancellationToken = default);
}