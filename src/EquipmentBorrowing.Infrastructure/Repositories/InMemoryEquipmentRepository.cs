using System.Collections.Concurrent;
using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly ConcurrentDictionary<int, Equipment> _equipment = new();
    private int _nextId;

    public void Seed(Equipment equipment)
    {
        if (equipment.Id == 0)
            EntityIdAssigner.Assign(equipment, Interlocked.Increment(ref _nextId));

        _equipment[equipment.Id] = equipment;
    }

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _equipment.TryGetValue(id, out var equipment);
        return Task.FromResult(equipment);
    }

    public Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Equipment> all = _equipment.Values.OrderBy(e => e.Id).ToList();
        return Task.FromResult(all);
    }

    public Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Equipment> available = _equipment.Values
            .Where(e => e.IsAvailable)
            .OrderBy(e => e.Id)
            .ToList();

        return Task.FromResult(available);
    }

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        _equipment[equipment.Id] = equipment;
        return Task.CompletedTask;
    }

    public Task<bool> IdentifierExistsAsync(
        string normalizedIdentifier,
        int? excludingId,
        CancellationToken cancellationToken = default)
    {
        var exists = _equipment.Values.Any(e =>
            string.Equals(e.Identifier, normalizedIdentifier, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || e.Id != excludingId));

        return Task.FromResult(exists);
    }

    public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        if (_equipment.Values.Any(e =>
                string.Equals(e.Identifier, equipment.Identifier, StringComparison.OrdinalIgnoreCase)))
            throw new DuplicateValueException("Equipment with that identifier already exists. No changes were made.");

        Seed(equipment);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateDetailsAsync(
        int id,
        string identifier,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (!_equipment.TryGetValue(id, out var equipment))
            return Task.FromResult(false);

        equipment.UpdateDetails(identifier, name, description);
        return Task.FromResult(true);
    }
}