using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

/// <summary>
/// Abstraction over student storage. The application layer only needs
/// to look a student up by id for the Borrow Equipment use case, so
/// that is the only method exposed here — methods are not added
/// speculatively.
/// </summary>
public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
