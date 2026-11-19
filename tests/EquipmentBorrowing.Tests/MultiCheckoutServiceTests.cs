using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class MultiCheckoutServiceTests
{
    private sealed record Fixture(
        MultiCheckoutService Multi,
        BorrowEquipmentService Single,
        InMemoryStudentRepository Students,
        InMemoryEquipmentRepository Equipment,
        InMemoryBorrowingRepository Borrowings);

    private static DateOnly NextWeek => DateOnly.FromDateTime(DateTime.Now.AddDays(7));

    private static Fixture Create()
    {
        var students = new InMemoryStudentRepository();
        var equipment = new InMemoryEquipmentRepository();
        var borrowings = new InMemoryBorrowingRepository();
        var transactions = new InMemoryBorrowingTransactions(students, equipment, borrowings);
        return new Fixture(
            new MultiCheckoutService(transactions),
            new BorrowEquipmentService(transactions),
            students,
            equipment,
            borrowings);
    }

    private static Student AddStudent(Fixture f, string number = "2024-0001")
    {
        var student = new Student(number, "Student " + number);
        f.Students.Seed(student);
        return student;
    }

    private static Equipment AddItem(Fixture f, string identifier)
    {
        var item = new Equipment(identifier, "Item " + identifier);
        f.Equipment.Seed(item);
        return item;
    }

    [Fact]
    public async Task TwoValidItems_CreatesTwoActiveBorrowings_AndMarksBothUnavailable()
    {
        var f = Create();
        var student = AddStudent(f);
        var a = AddItem(f, "EQ-1");
        var b = AddItem(f, "EQ-2");

        var result = await f.Multi.CheckoutAsync(student.Id, new[] { a.Id, b.Id }, NextWeek);

        Assert.True(result.Success);
        Assert.Equal(2, result.Borrowings.Count);
        Assert.False(a.IsAvailable);
        Assert.False(b.IsAvailable);
        Assert.Equal(2, await f.Borrowings.CountActiveByStudentAsync(student.Id));
    }

    [Fact]
    public async Task SingleItem_IsRejected()
    {
        var f = Create();
        var student = AddStudent(f);
        var a = AddItem(f, "EQ-1");

        var result = await f.Multi.CheckoutAsync(student.Id, new[] { a.Id }, NextWeek);

        Assert.False(result.Success);
        Assert.True(a.IsAvailable);
        Assert.Equal(0, await f.Borrowings.CountActiveByStudentAsync(student.Id));
    }

    [Fact]
    public async Task DuplicateItems_AreRejected()
    {
        var f = Create();
        var student = AddStudent(f);
        var a = AddItem(f, "EQ-1");

        var result = await f.Multi.CheckoutAsync(student.Id, new[] { a.Id, a.Id }, NextWeek);

        Assert.False(result.Success);
        Assert.True(a.IsAvailable);
        Assert.Equal(0, await f.Borrowings.CountActiveByStudentAsync(student.Id));
    }

    [Fact]
    public async Task UnavailableItem_RejectsTheWholeRequest()
    {
        var f = Create();
        var student = AddStudent(f);
        var a = AddItem(f, "EQ-1");
        var b = AddItem(f, "EQ-2");
        b.MarkUnavailable();

        var result = await f.Multi.CheckoutAsync(student.Id, new[] { a.Id, b.Id }, NextWeek);

        Assert.False(result.Success);
        Assert.True(a.IsAvailable);
        Assert.Equal(0, await f.Borrowings.CountActiveByStudentAsync(student.Id));
    }

    [Fact]
    public async Task MissingItem_RejectsTheWholeRequest()
    {
        var f = Create();
        var student = AddStudent(f);
        var a = AddItem(f, "EQ-1");

        var result = await f.Multi.CheckoutAsync(student.Id, new[] { a.Id, 999 }, NextWeek);

        Assert.False(result.Success);
        Assert.True(a.IsAvailable);
        Assert.Equal(0, await f.Borrowings.CountActiveByStudentAsync(student.Id));
    }

    [Fact]
    public async Task MissingStudent_IsRejected()
    {
        var f = Create();
        var a = AddItem(f, "EQ-1");
        var b = AddItem(f, "EQ-2");

        var result = await f.Multi.CheckoutAsync(999, new[] { a.Id, b.Id }, NextWeek);

        Assert.False(result.Success);
        Assert.True(a.IsAvailable);
        Assert.True(b.IsAvailable);
    }

    [Fact]
    public async Task IneligibleStudent_IsRejected()
    {
        var f = Create();
        var student = AddStudent(f);
        student.Suspend();
        var a = AddItem(f, "EQ-1");
        var b = AddItem(f, "EQ-2");

        var result = await f.Multi.CheckoutAsync(student.Id, new[] { a.Id, b.Id }, NextWeek);

        Assert.False(result.Success);
        Assert.True(a.IsAvailable);
        Assert.True(b.IsAvailable);
    }

    [Fact]
    public async Task ReturnDateNotLaterThanToday_IsRejected()
    {
        var f = Create();
        var student = AddStudent(f);
        var a = AddItem(f, "EQ-1");
        var b = AddItem(f, "EQ-2");

        var result = await f.Multi.CheckoutAsync(
            student.Id, new[] { a.Id, b.Id }, DateOnly.FromDateTime(DateTime.Now));

        Assert.False(result.Success);
        Assert.True(a.IsAvailable);
        Assert.True(b.IsAvailable);
    }

    [Fact]
    public async Task ExactlyAtLimit_Succeeds()
    {
        var f = Create();
        var student = AddStudent(f);
        var first = AddItem(f, "EQ-1");
        var b = AddItem(f, "EQ-2");
        var c = AddItem(f, "EQ-3");
        Assert.True((await f.Single.BorrowAsync(student.Id, first.Id, NextWeek)).Success);

        var result = await f.Multi.CheckoutAsync(student.Id, new[] { b.Id, c.Id }, NextWeek);

        Assert.True(result.Success);
        Assert.Equal(3, await f.Borrowings.CountActiveByStudentAsync(student.Id));
    }

    [Fact]
    public async Task OneOverTheLimit_IsRejected_WithNoChanges()
    {
        var f = Create();
        var student = AddStudent(f);
        var first = AddItem(f, "EQ-1");
        var second = AddItem(f, "EQ-2");
        var c = AddItem(f, "EQ-3");
        var d = AddItem(f, "EQ-4");
        Assert.True((await f.Single.BorrowAsync(student.Id, first.Id, NextWeek)).Success);
        Assert.True((await f.Single.BorrowAsync(student.Id, second.Id, NextWeek)).Success);

        var result = await f.Multi.CheckoutAsync(student.Id, new[] { c.Id, d.Id }, NextWeek);

        Assert.False(result.Success);
        Assert.True(c.IsAvailable);
        Assert.True(d.IsAvailable);
        Assert.Equal(2, await f.Borrowings.CountActiveByStudentAsync(student.Id));
    }
}
