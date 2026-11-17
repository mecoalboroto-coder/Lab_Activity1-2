using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        EquipmentBorrowingDbContext context,
        CancellationToken cancellationToken = default)
    {
        if (await context.Students.AnyAsync(cancellationToken))
            return;

        var students = new[]
        {
            new Student(1, "Juan Dela Cruz"),
            new Student(2, "Maria Santos"),
            new Student(3, "Pedro Reyes", isAllowedToBorrow: false)
        };

        var equipment = new[]
        {
            new Equipment(100, "Digital Multimeter", "Measuring instrument"),
            new Equipment(101, "Oscilloscope", "Waveform display", isAvailable: false),
            new Equipment(102, "Soldering Iron", "Electronics tool"),
            new Equipment(103, "Function Generator", "Signal generator")
        };

        context.Students.AddRange(students);
        context.Equipment.AddRange(equipment);
        await context.SaveChangesAsync(cancellationToken);

        var existingBorrowing = new Borrowing(
            id: 9000,
            studentId: 2,
            equipmentId: 101,
            dateBorrowed: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            expectedReturnDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));

        context.Borrowings.Add(existingBorrowing);
        await context.SaveChangesAsync(cancellationToken);
    }
}