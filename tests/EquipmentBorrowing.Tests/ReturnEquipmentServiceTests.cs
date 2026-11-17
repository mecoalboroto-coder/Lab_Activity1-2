using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class ReturnEquipmentServiceTests
{
    [Fact]
    public async Task ReturnAsync_WithActiveBorrowing_Succeeds_AndMakesEquipmentAvailable()
    {
        var students = new InMemoryStudentRepository();
        var equipment = new InMemoryEquipmentRepository();
        var borrowings = new InMemoryBorrowingRepository();

        students.Seed(new Student(1, "Juan Dela Cruz"));
        equipment.Seed(new Equipment(100, "Multimeter"));

        var borrowService = new BorrowEquipmentService(students, equipment, borrowings);
        var borrowResult = await borrowService.BorrowAsync(1, 100, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
        Assert.True(borrowResult.Success);

        var returnService = new ReturnEquipmentService(equipment, borrowings);
        var returnResult = await returnService.ReturnAsync(borrowResult.Borrowing!.Id);

        Assert.True(returnResult.Success);
        Assert.Equal(BorrowingStatus.Returned, returnResult.Borrowing!.Status);
        Assert.NotNull(returnResult.Borrowing.ReturnedAt);

        var refreshedEquipment = await equipment.GetByIdAsync(100);
        Assert.True(refreshedEquipment!.IsAvailable);
    }

    [Fact]
    public async Task ReturnAsync_WhenBorrowingDoesNotExist_Fails()
    {
        var equipment = new InMemoryEquipmentRepository();
        var borrowings = new InMemoryBorrowingRepository();
        var returnService = new ReturnEquipmentService(equipment, borrowings);

        var result = await returnService.ReturnAsync(999);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReturnAsync_WhenAlreadyReturned_Fails()
    {
        var students = new InMemoryStudentRepository();
        var equipment = new InMemoryEquipmentRepository();
        var borrowings = new InMemoryBorrowingRepository();

        students.Seed(new Student(1, "Juan Dela Cruz"));
        equipment.Seed(new Equipment(100, "Multimeter"));

        var borrowService = new BorrowEquipmentService(students, equipment, borrowings);
        var borrowResult = await borrowService.BorrowAsync(1, 100, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));

        var returnService = new ReturnEquipmentService(equipment, borrowings);
        await returnService.ReturnAsync(borrowResult.Borrowing!.Id);

        var secondAttempt = await returnService.ReturnAsync(borrowResult.Borrowing!.Id);

        Assert.False(secondAttempt.Success);
    }
}