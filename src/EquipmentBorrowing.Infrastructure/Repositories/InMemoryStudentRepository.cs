using System.Collections.Concurrent;
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
}