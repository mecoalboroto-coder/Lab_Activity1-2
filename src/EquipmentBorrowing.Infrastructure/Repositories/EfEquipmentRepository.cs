using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfEquipmentRepository : IEquipmentRepository
{
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
}