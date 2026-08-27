using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class BorrowEquipmentServiceTests
{
    private static (BorrowEquipmentService Service, InMemoryStudentRepository Students,
        InMemoryEquipmentRepository Equipment, InMemoryBorrowingRepository Borrowings) CreateService(
            int maxActiveBorrowings = 3)
    {
        var students = new InMemoryStudentRepository();
        var equipment = new InMemoryEquipmentRepository();
        var borrowings = new InMemoryBorrowingRepository();
        var service = new BorrowEquipmentService(students, equipment, borrowings, maxActiveBorrowings);
        return (service, students, equipment, borrowings);
    }

    [Fact]
    public async Task BorrowAsync_WithEligibleStudentAndAvailableEquipment_Succeeds()
    {
        var (service, students, equipment, _) = CreateService();
        students.Seed(new Student(1, "Juan Dela Cruz"));
        equipment.Seed(new Equipment(100, "Multimeter"));

        var result = await service.BorrowAsync(1, 100, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));

        Assert.True(result.Success);
        Assert.NotNull(result.Borrowing);
        Assert.Equal(BorrowingStatus.Active, result.Borrowing!.Status);
    }

    [Fact]
    public async Task BorrowAsync_WhenStudentDoesNotExist_Fails()
    {
        var (service, _, equipment, _) = CreateService();
        equipment.Seed(new Equipment(100, "Multimeter"));

        var result = await service.BorrowAsync(999, 100, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Null(result.Borrowing);
    }

    [Fact]
    public async Task BorrowAsync_WhenStudentNotAllowedToBorrow_Fails()
    {
        var (service, students, equipment, _) = CreateService();
        students.Seed(new Student(1, "Maria Santos", isAllowedToBorrow: false));
        equipment.Seed(new Equipment(100, "Multimeter"));

        var result = await service.BorrowAsync(1, 100, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task BorrowAsync_WhenEquipmentDoesNotExist_Fails()
    {
        var (service, students, _, _) = CreateService();
        students.Seed(new Student(1, "Juan Dela Cruz"));

        var result = await service.BorrowAsync(1, 999, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task BorrowAsync_WhenEquipmentUnavailable_Fails()
    {
        var (service, students, equipment, _) = CreateService();
        students.Seed(new Student(1, "Juan Dela Cruz"));
        equipment.Seed(new Equipment(100, "Multimeter", isAvailable: false));

        var result = await service.BorrowAsync(1, 100, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task BorrowAsync_WhenStudentAtMaxActiveBorrowings_Fails()
    {
        var (service, students, equipment, _) = CreateService(maxActiveBorrowings: 1);
        students.Seed(new Student(1, "Juan Dela Cruz"));
        equipment.Seed(new Equipment(100, "Multimeter"));
        equipment.Seed(new Equipment(101, "Oscilloscope"));

        var returnDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var first = await service.BorrowAsync(1, 100, returnDate);
        Assert.True(first.Success);

        var second = await service.BorrowAsync(1, 101, returnDate);

        Assert.False(second.Success);
    }
}
