using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class BorrowingHistoryServiceTests
{
    private sealed class StubQueries : IBorrowingQueries
    {
        public bool Throw { get; set; }
        public List<Borrowing> Rows { get; } = new();

        public Task<IReadOnlyList<Borrowing>> GetByStudentAsync(int studentId, CancellationToken cancellationToken = default)
        {
            if (Throw)
                throw new InvalidOperationException("db down");

            IReadOnlyList<Borrowing> result = Rows.Where(b => b.StudentId == studentId).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Borrowing>> GetByEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default)
        {
            if (Throw)
                throw new InvalidOperationException("db down");

            IReadOnlyList<Borrowing> result = Rows.Where(b => b.EquipmentId == equipmentId).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Borrowing>> SearchAsync(BorrowingSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            if (Throw)
                throw new InvalidOperationException("db down");

            IReadOnlyList<Borrowing> result = Rows.ToList();
            return Task.FromResult(result);
        }
    }

    private static (BorrowingHistoryService Service, StubQueries Queries, InMemoryStudentRepository Students,
        InMemoryEquipmentRepository Equipment) Create()
    {
        var students = new InMemoryStudentRepository();
        var equipment = new InMemoryEquipmentRepository();
        var queries = new StubQueries();
        return (new BorrowingHistoryService(students, equipment, queries), queries, students, equipment);
    }

    private static Borrowing Row(int studentId, int equipmentId) =>
        new(studentId, equipmentId, DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(7)));

    [Fact]
    public async Task StudentHistory_ReturnsOnlyThatStudentsRecords()
    {
        var (service, queries, students, _) = Create();
        var one = new Student("2024-0001", "One");
        var two = new Student("2024-0002", "Two");
        students.Seed(one);
        students.Seed(two);
        queries.Rows.Add(Row(one.Id, 10));
        queries.Rows.Add(Row(two.Id, 11));

        var result = await service.ForStudentAsync(one.Id);

        Assert.Equal(QueryState.Loaded, result.State);
        Assert.Single(result.Items);
        Assert.All(result.Items, b => Assert.Equal(one.Id, b.StudentId));
    }

    [Fact]
    public async Task StudentHistory_WithNoRecords_IsLoadedAndEmpty()
    {
        var (service, _, students, _) = Create();
        var student = new Student("2024-0001", "One");
        students.Seed(student);

        var result = await service.ForStudentAsync(student.Id);

        Assert.Equal(QueryState.Loaded, result.State);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task StudentHistory_ForMissingStudent_IsMissing()
    {
        var (service, _, _, _) = Create();

        var result = await service.ForStudentAsync(999);

        Assert.Equal(QueryState.Missing, result.State);
    }

    [Fact]
    public async Task DatabaseFailure_IsReportedAsFailed_WithoutLeakingDetails()
    {
        var (service, queries, students, _) = Create();
        var student = new Student("2024-0001", "One");
        students.Seed(student);
        queries.Throw = true;

        var result = await service.ForStudentAsync(student.Id);

        Assert.Equal(QueryState.Failed, result.State);
        Assert.DoesNotContain("db down", result.Message);
    }

    [Fact]
    public async Task EquipmentHistory_ForMissingItem_IsMissing()
    {
        var (service, _, _, _) = Create();

        var result = await service.ForEquipmentAsync(999);

        Assert.Equal(QueryState.Missing, result.State);
    }
}
