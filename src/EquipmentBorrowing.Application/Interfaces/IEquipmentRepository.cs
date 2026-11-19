using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);

    Task<bool> IdentifierExistsAsync(string normalizedIdentifier, int? excludingId, CancellationToken cancellationToken = default);

    Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default);

    Task<bool> UpdateDetailsAsync(
        int id,
        string identifier,
        string name,
        string? description,
        CancellationToken cancellationToken = default);
}