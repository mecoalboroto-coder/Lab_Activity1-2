using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        EquipmentBorrowingDbContext context,
        CancellationToken cancellationToken = default)
    {
        if (await context.Students.AnyAsync(cancellationToken)
            || await context.Equipment.AnyAsync(cancellationToken))
            return;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var students = new[]
        {
            new Student("2024-0001", "Juan Dela Cruz", "juan@example.com"),
            new Student("2024-0002", "Maria Santos"),
            new Student("2024-0003", "Pedro Reyes")
        };
        students[2].Suspend();

        var equipment = new[]
        {
            new Equipment("EQ-100", "Digital Multimeter", "Measuring instrument"),
            new Equipment("EQ-101", "Oscilloscope", "Waveform display"),
            new Equipment("EQ-102", "Soldering Iron", "Electronics tool"),
            new Equipment("EQ-103", "Function Generator", "Signal generator")
        };
        equipment[1].MarkUnavailable();

        context.Students.AddRange(students);
        context.Equipment.AddRange(equipment);
        await context.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Now);
        context.Borrowings.Add(new Borrowing(
            students[1].Id,
            equipment[1].Id,
            today.AddDays(-2),
            today.AddDays(5)));
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}