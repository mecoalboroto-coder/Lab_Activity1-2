using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class ReturnEquipmentServiceTests
{
    private static DateOnly NextWeek => DateOnly.FromDateTime(DateTime.Now.AddDays(7));

    private static (BorrowEquipmentService Borrow, ReturnEquipmentService Return,
        InMemoryStudentRepository Students, InMemoryEquipmentRepository Equipment) CreateServices()
    {
        var students = new InMemoryStudentRepository();
        var equipment = new InMemoryEquipmentRepository();
        var borrowings = new InMemoryBorrowingRepository();
        var transactions = new InMemoryBorrowingTransactions(students, equipment, borrowings);
        return (new BorrowEquipmentService(transactions), new ReturnEquipmentService(transactions), students, equipment);
    }

    [Fact]
    public async Task ReturnAsync_WithActiveBorrowing_Succeeds_AndMakesEquipmentAvailable()
    {
        var (borrow, returns, students, equipment) = CreateServices();
        var student = new Student("2024-0001", "Juan Dela Cruz");
        var item = new Equipment("EQ-100", "Multimeter");
        students.Seed(student);
        equipment.Seed(item);

        var borrowResult = await borrow.BorrowAsync(student.Id, item.Id, NextWeek);
        Assert.True(borrowResult.Success);

        var returnResult = await returns.ReturnAsync(borrowResult.Borrowing!.Id);

        Assert.True(returnResult.Success);
        Assert.Equal(BorrowingStatus.Returned, returnResult.Borrowing!.Status);
        Assert.NotNull(returnResult.Borrowing.ReturnedAt);
        Assert.True((await equipment.GetByIdAsync(item.Id))!.IsAvailable);
    }

    [Fact]
    public async Task ReturnAsync_WhenBorrowingDoesNotExist_Fails()
    {
        var (_, returns, _, _) = CreateServices();

        var result = await returns.ReturnAsync(999);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReturnAsync_WhenAlreadyReturned_Fails()
    {
        var (borrow, returns, students, equipment) = CreateServices();
        var student = new Student("2024-0001", "Juan Dela Cruz");
        var item = new Equipment("EQ-100", "Multimeter");
        students.Seed(student);
        equipment.Seed(item);

        var borrowResult = await borrow.BorrowAsync(student.Id, item.Id, NextWeek);
        var borrowingId = borrowResult.Borrowing!.Id;
        await returns.ReturnAsync(borrowingId);

        var secondAttempt = await returns.ReturnAsync(borrowingId);

        Assert.False(secondAttempt.Success);
    }
}