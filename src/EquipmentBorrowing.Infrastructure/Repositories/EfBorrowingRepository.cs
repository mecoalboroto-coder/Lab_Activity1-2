using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfBorrowingRepository : IBorrowingRepository
{
    private readonly IDbContextFactory<EquipmentBorrowingDbContext> _dbContextFactory;

    public EfBorrowingRepository(IDbContextFactory<EquipmentBorrowingDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        context.Borrowings.Add(borrowing);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountActiveByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Borrowings
            .AsNoTracking()
            .CountAsync(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active, cancellationToken);
    }

    public async Task<Borrowing?> GetActiveByStudentAndEquipmentAsync(
        int studentId,
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Borrowings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.StudentId == studentId
                     && b.EquipmentId == equipmentId
                     && b.Status == BorrowingStatus.Active,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Borrowing>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .Where(b => b.Status == BorrowingStatus.Active)
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Borrowings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tracked = await context.Borrowings.FindAsync(new object?[] { borrowing.Id }, cancellationToken);

        if (tracked is null)
            return;

        context.Entry(tracked).CurrentValues.SetValues(borrowing);
        await context.SaveChangesAsync(cancellationToken);
    }
}