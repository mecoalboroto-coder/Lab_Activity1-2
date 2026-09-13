using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default);
}