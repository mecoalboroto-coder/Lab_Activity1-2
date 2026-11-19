using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfStudentRepository : IStudentRepository
{
    private const string DuplicateMessage =
        "A student with that number already exists. No changes were made.";

    private readonly IDbContextFactory<EquipmentBorrowingDbContext> _dbContextFactory;

    public EfStudentRepository(IDbContextFactory<EquipmentBorrowingDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Students
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> NumberExistsAsync(
        string normalizedNumber,
        int? excludingId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var key = normalizedNumber.ToLower();

        return await context.Students
            .AsNoTracking()
            .AnyAsync(
                s => s.StudentNumber.ToLower() == key && (excludingId == null || s.Id != excludingId),
                cancellationToken);
    }

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        context.Students.Add(student);
        await PersistenceErrors.SaveAsync(context, DuplicateMessage, cancellationToken);
    }

    public async Task<bool> UpdateDetailsAsync(
        int id,
        string studentNumber,
        string name,
        string? contact,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tracked = await context.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (tracked is null)
            return false;

        tracked.UpdateDetails(studentNumber, name, contact);
        await PersistenceErrors.SaveAsync(context, DuplicateMessage, cancellationToken);
        return true;
    }

    public async Task<EligibilityChange> SetEligibilityAsync(
        int id,
        bool allowed,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tracked = await context.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (tracked is null)
            return EligibilityChange.NotFound;

        if (tracked.IsAllowedToBorrow == allowed)
            return EligibilityChange.Unchanged;

        if (allowed)
            tracked.Reinstate();
        else
            tracked.Suspend();

        await PersistenceErrors.SaveAsync(context, DuplicateMessage, cancellationToken);
        return EligibilityChange.Changed;
    }
}