using System.Collections.Concurrent;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly ConcurrentDictionary<int, Borrowing> _borrowings = new();

    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        _borrowings[borrowing.Id] = borrowing;
        return Task.CompletedTask;
    }

    public Task<int> CountActiveByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var count = _borrowings.Values.Count(b =>
            b.StudentId == studentId && b.Status == BorrowingStatus.Active);

        return Task.FromResult(count);
    }

    public Task<Borrowing?> GetActiveByStudentAndEquipmentAsync(
        int studentId,
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        var match = _borrowings.Values.FirstOrDefault(b =>
            b.StudentId == studentId &&
            b.EquipmentId == equipmentId &&
            b.Status == BorrowingStatus.Active);

        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<Borrowing>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Borrowing> active = _borrowings.Values
            .Where(b => b.Status == BorrowingStatus.Active)
            .OrderBy(b => b.Id)
            .ToList();

        return Task.FromResult(active);
    }

    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _borrowings.TryGetValue(id, out var borrowing);
        return Task.FromResult(borrowing);
    }

    public Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        _borrowings[borrowing.Id] = borrowing;
        return Task.CompletedTask;
    }
}