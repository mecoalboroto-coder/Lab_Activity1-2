using System.Collections.Concurrent;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

/// <summary>
/// Simple in-memory implementation of <see cref="IStudentRepository"/>.
/// Demonstrates that the application layer can operate without knowing
/// how data is actually stored. No database is used.
/// </summary>
public class InMemoryStudentRepository : IStudentRepository
{
    private readonly ConcurrentDictionary<int, Student> _students = new();

    public void Seed(Student student) => _students[student.Id] = student;

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _students.TryGetValue(id, out var student);
        return Task.FromResult(student);
    }
}
