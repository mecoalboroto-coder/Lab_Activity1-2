using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfEquipmentRepository : IEquipmentRepository
{
    private const string DuplicateMessage =
        "Equipment with that identifier already exists. No changes were made.";

    private readonly IDbContextFactory<EquipmentBorrowingDbContext> _dbContextFactory;

    public EfEquipmentRepository(IDbContextFactory<EquipmentBorrowingDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Equipment
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Equipment
            .AsNoTracking()
            .Where(e => e.IsAvailable)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tracked = await context.Equipment.FindAsync(new object?[] { equipment.Id }, cancellationToken);

        if (tracked is null)
            return;

        context.Entry(tracked).CurrentValues.SetValues(equipment);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IdentifierExistsAsync(
        string normalizedIdentifier,
        int? excludingId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var key = normalizedIdentifier.ToLower();

        return await context.Equipment
            .AsNoTracking()
            .AnyAsync(
                e => e.Identifier.ToLower() == key && (excludingId == null || e.Id != excludingId),
                cancellationToken);
    }

    public async Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        context.Equipment.Add(equipment);
        await PersistenceErrors.SaveAsync(context, DuplicateMessage, cancellationToken);
    }

    public async Task<bool> UpdateDetailsAsync(
        int id,
        string identifier,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tracked = await context.Equipment.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (tracked is null)
            return false;

        tracked.UpdateDetails(identifier, name, description);
        await PersistenceErrors.SaveAsync(context, DuplicateMessage, cancellationToken);
        return true;
    }
}