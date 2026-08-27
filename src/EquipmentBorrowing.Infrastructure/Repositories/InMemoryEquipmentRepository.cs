using System.Collections.Concurrent;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

/// <summary>
/// Simple in-memory implementation of <see cref="IEquipmentRepository"/>.
/// </summary>
public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly ConcurrentDictionary<int, Equipment> _equipment = new();

    public void Seed(Equipment equipment) => _equipment[equipment.Id] = equipment;

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _equipment.TryGetValue(id, out var equipment);
        return Task.FromResult(equipment);
    }

    public Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Equipment> available = _equipment.Values
            .Where(e => e.IsAvailable)
            .ToList();

        return Task.FromResult(available);
    }

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        _equipment[equipment.Id] = equipment;
        return Task.CompletedTask;
    }
}
