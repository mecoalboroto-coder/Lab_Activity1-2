using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class BorrowEquipmentServiceTests
{
    private static DateOnly NextWeek => DateOnly.FromDateTime(DateTime.Now.AddDays(7));

    private static (BorrowEquipmentService Service, InMemoryStudentRepository Students,
        InMemoryEquipmentRepository Equipment) CreateService()
    {
        var students = new InMemoryStudentRepository();
        var equipment = new InMemoryEquipmentRepository();
        var borrowings = new InMemoryBorrowingRepository();
        var transactions = new InMemoryBorrowingTransactions(students, equipment, borrowings);
        return (new BorrowEquipmentService(transactions), students, equipment);
    }

    [Fact]
    public async Task BorrowAsync_WithEligibleStudentAndAvailableEquipment_Succeeds()
    {
        var (service, students, equipment) = CreateService();
        var student = new Student("2024-0001", "Juan Dela Cruz");
        var item = new Equipment("EQ-100", "Multimeter");
        students.Seed(student);
        equipment.Seed(item);

        var result = await service.BorrowAsync(student.Id, item.Id, NextWeek);

        Assert.True(result.Success);
        Assert.NotNull(result.Borrowing);
        Assert.Equal(BorrowingStatus.Active, result.Borrowing!.Status);
        Assert.False((await equipment.GetByIdAsync(item.Id))!.IsAvailable);
    }

    [Fact]
    public async Task BorrowAsync_WhenStudentDoesNotExist_Fails()
    {
        var (service, _, equipment) = CreateService();
        var item = new Equipment("EQ-100", "Multimeter");
        equipment.Seed(item);

        var result = await service.BorrowAsync(999, item.Id, NextWeek);

        Assert.False(result.Success);
        Assert.Null(result.Borrowing);
    }

    [Fact]
    public async Task BorrowAsync_WhenStudentNotAllowedToBorrow_Fails()
    {
        var (service, students, equipment) = CreateService();
        var student = new Student("2024-0002", "Maria Santos");
        student.Suspend();
        var item = new Equipment("EQ-100", "Multimeter");
        students.Seed(student);
        equipment.Seed(item);

        var result = await service.BorrowAsync(student.Id, item.Id, NextWeek);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task BorrowAsync_WhenEquipmentDoesNotExist_Fails()
    {
        var (service, students, _) = CreateService();
        var student = new Student("2024-0001", "Juan Dela Cruz");
        students.Seed(student);

        var result = await service.BorrowAsync(student.Id, 999, NextWeek);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task BorrowAsync_WhenEquipmentUnavailable_Fails()
    {
        var (service, students, equipment) = CreateService();
        var student = new Student("2024-0001", "Juan Dela Cruz");
        var item = new Equipment("EQ-100", "Multimeter");
        item.MarkUnavailable();
        students.Seed(student);
        equipment.Seed(item);

        var result = await service.BorrowAsync(student.Id, item.Id, NextWeek);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task BorrowAsync_WhenExpectedDateIsNotLaterThanToday_Fails()
    {
        var (service, students, equipment) = CreateService();
        var student = new Student("2024-0001", "Juan Dela Cruz");
        var item = new Equipment("EQ-100", "Multimeter");
        students.Seed(student);
        equipment.Seed(item);

        var result = await service.BorrowAsync(student.Id, item.Id, DateOnly.FromDateTime(DateTime.Now));

        Assert.False(result.Success);
        Assert.True((await equipment.GetByIdAsync(item.Id))!.IsAvailable);
    }

    [Fact]
    public async Task BorrowAsync_WhenStudentAtActiveLimit_Fails()
    {
        var (service, students, equipment) = CreateService();
        var student = new Student("2024-0001", "Juan Dela Cruz");
        students.Seed(student);

        var items = Enumerable.Range(1, 4)
            .Select(i => new Equipment($"EQ-{i}", $"Item {i}"))
            .ToList();
        foreach (var item in items)
            equipment.Seed(item);

        for (var i = 0; i < CheckoutPolicy.MaxActiveBorrowings; i++)
            Assert.True((await service.BorrowAsync(student.Id, items[i].Id, NextWeek)).Success);

        var overLimit = await service.BorrowAsync(student.Id, items[3].Id, NextWeek);

        Assert.False(overLimit.Success);
    }
}