using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

var studentRepository = new InMemoryStudentRepository();
var equipmentRepository = new InMemoryEquipmentRepository();
var borrowingRepository = new InMemoryBorrowingRepository();
var transactions = new InMemoryBorrowingTransactions(studentRepository, equipmentRepository, borrowingRepository);

var juan = new Student("2024-0001", "Juan Dela Cruz");
var maria = new Student("2024-0002", "Maria Santos");
maria.Suspend();
studentRepository.Seed(juan);
studentRepository.Seed(maria);

var multimeter = new Equipment("EQ-100", "Digital Multimeter", "Measuring instrument");
var oscilloscope = new Equipment("EQ-101", "Oscilloscope", "Waveform display");
oscilloscope.MarkUnavailable();
equipmentRepository.Seed(multimeter);
equipmentRepository.Seed(oscilloscope);

var borrowService = new BorrowEquipmentService(transactions);
var returnService = new ReturnEquipmentService(transactions);

var returnDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7));

Console.WriteLine("=== Case 1: Successful borrow ===");
var success = await borrowService.BorrowAsync(juan.Id, multimeter.Id, returnDate);
PrintResult(success);

Console.WriteLine();
Console.WriteLine("=== Case 2: Failure - equipment already unavailable ===");
PrintResult(await borrowService.BorrowAsync(juan.Id, oscilloscope.Id, returnDate));

Console.WriteLine();
Console.WriteLine("=== Case 3: Failure - student not allowed to borrow ===");
PrintResult(await borrowService.BorrowAsync(maria.Id, multimeter.Id, returnDate));

Console.WriteLine();
Console.WriteLine("=== Case 4: Failure - equipment does not exist ===");
PrintResult(await borrowService.BorrowAsync(juan.Id, 999, returnDate));

Console.WriteLine();
Console.WriteLine("=== Bonus: Return the equipment from Case 1 ===");
if (success.Borrowing is not null)
{
    var returned = await returnService.ReturnAsync(success.Borrowing.Id);
    Console.WriteLine(returned.Success ? "Equipment returned successfully." : $"Return failed: {returned.FailureReason}");
}

static void PrintResult(BorrowResult result)
{
    if (result.Success)
    {
        var borrowing = result.Borrowing!;
        Console.WriteLine(
            $"SUCCESS - Borrowing #{borrowing.Id} created " +
            $"(Student {borrowing.StudentId}, Equipment {borrowing.EquipmentId}, Status: {borrowing.Status}).");
    }
    else
    {
        Console.WriteLine($"FAILED - {result.FailureReason}");
    }
}