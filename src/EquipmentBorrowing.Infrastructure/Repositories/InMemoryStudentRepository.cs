using System.Collections.Concurrent;
using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly ConcurrentDictionary<int, Student> _students = new();
    private int _nextId;

    public void Seed(Student student)
    {
        if (student.Id == 0)
            EntityIdAssigner.Assign(student, Interlocked.Increment(ref _nextId));

        _students[student.Id] = student;
    }

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _students.TryGetValue(id, out var student);
        return Task.FromResult(student);
    }

    public Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Student> all = _students.Values.OrderBy(s => s.Id).ToList();
        return Task.FromResult(all);
    }

    public Task<bool> NumberExistsAsync(
        string normalizedNumber,
        int? excludingId,
        CancellationToken cancellationToken = default)
    {
        var exists = _students.Values.Any(s =>
            string.Equals(s.StudentNumber, normalizedNumber, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || s.Id != excludingId));

        return Task.FromResult(exists);
    }

    public Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        if (_students.Values.Any(s =>
                string.Equals(s.StudentNumber, student.StudentNumber, StringComparison.OrdinalIgnoreCase)))
            throw new DuplicateValueException("A student with that number already exists. No changes were made.");

        Seed(student);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateDetailsAsync(
        int id,
        string studentNumber,
        string name,
        string? contact,
        CancellationToken cancellationToken = default)
    {
        if (!_students.TryGetValue(id, out var student))
            return Task.FromResult(false);

        student.UpdateDetails(studentNumber, name, contact);
        return Task.FromResult(true);
    }

    public Task<EligibilityChange> SetEligibilityAsync(
        int id,
        bool allowed,
        CancellationToken cancellationToken = default)
    {
        if (!_students.TryGetValue(id, out var student))
            return Task.FromResult(EligibilityChange.NotFound);

        if (student.IsAllowedToBorrow == allowed)
            return Task.FromResult(EligibilityChange.Unchanged);

        if (allowed)
            student.Reinstate();
        else
            student.Suspend();

        return Task.FromResult(EligibilityChange.Changed);
    }
}