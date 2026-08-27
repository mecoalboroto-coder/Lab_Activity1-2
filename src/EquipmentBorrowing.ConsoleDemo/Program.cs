using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

// This program only wires dependencies together and prints results.
// It contains no business rules — those live in BorrowEquipmentService.

var studentRepository = new InMemoryStudentRepository();
var equipmentRepository = new InMemoryEquipmentRepository();
var borrowingRepository = new InMemoryBorrowingRepository();

studentRepository.Seed(new Student(1, "Juan Dela Cruz", isAllowedToBorrow: true));
studentRepository.Seed(new Student(2, "Maria Santos", isAllowedToBorrow: false)); // suspended

equipmentRepository.Seed(new Equipment(100, "Digital Multimeter", isAvailable: true));
equipmentRepository.Seed(new Equipment(101, "Oscilloscope", isAvailable: false)); // already borrowed

var borrowService = new BorrowEquipmentService(
    studentRepository, equipmentRepository, borrowingRepository, maxActiveBorrowingsPerStudent: 3);

var returnService = new ReturnEquipmentService(equipmentRepository, borrowingRepository);

var returnDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

Console.WriteLine("=== Case 1: Successful borrow ===");
var success = await borrowService.BorrowAsync(studentId: 1, equipmentId: 100, returnDate);
PrintResult(success);

Console.WriteLine();
Console.WriteLine("=== Case 2: Failure — equipment already unavailable ===");
var failUnavailable = await borrowService.BorrowAsync(studentId: 1, equipmentId: 101, returnDate);
PrintResult(failUnavailable);

Console.WriteLine();
Console.WriteLine("=== Case 3: Failure — student not allowed to borrow ===");
var failSuspended = await borrowService.BorrowAsync(studentId: 2, equipmentId: 100, returnDate);
PrintResult(failSuspended);

Console.WriteLine();
Console.WriteLine("=== Case 4: Failure — equipment does not exist ===");
var failMissing = await borrowService.BorrowAsync(studentId: 1, equipmentId: 999, returnDate);
PrintResult(failMissing);

Console.WriteLine();
Console.WriteLine("=== Bonus: Return the equipment from Case 1, then confirm it's available again ===");
var returned = await returnService.ReturnAsync(studentId: 1, equipmentId: 100);
Console.WriteLine(returned ? "Equipment returned successfully." : "Return failed.");

var equipmentAfterReturn = await equipmentRepository.GetByIdAsync(100);
Console.WriteLine($"Equipment 100 available again: {equipmentAfterReturn?.IsAvailable}");

static void PrintResult(BorrowResult result)
{
    if (result.Success)
    {
        Console.WriteLine(
            $"SUCCESS — Borrowing #{result.Borrowing!.Id} created " +
            $"(Student {result.Borrowing.StudentId}, Equipment {result.Borrowing.EquipmentId}, " +
            $"Status: {result.Borrowing.Status}).");
    }
    else
    {
        Console.WriteLine($"FAILED — {result.FailureReason}");
    }
}
