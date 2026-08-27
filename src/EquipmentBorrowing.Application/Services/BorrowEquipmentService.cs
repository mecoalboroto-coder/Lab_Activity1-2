using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

/// <summary>
/// Coordinates the "Borrow Equipment" use case. This class contains the
/// actual business operation requested by the Student actor: it checks
/// every borrowing rule from the scenario, and only if all of them pass
/// does it create and persist a new Borrowing.
///
/// It depends only on repository *interfaces*, received through the
/// constructor (manual dependency injection) — it never creates a
/// database connection, executes SQL, or contains UI code.
/// </summary>
public class BorrowEquipmentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly int _maxActiveBorrowingsPerStudent;

    public BorrowEquipmentService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository,
        int maxActiveBorrowingsPerStudent = 3)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
        _maxActiveBorrowingsPerStudent = maxActiveBorrowingsPerStudent;
    }

    public async Task<BorrowResult> BorrowAsync(
        int studentId,
        int equipmentId,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        // 1. Does the student exist?
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            return BorrowResult.Failed($"Student {studentId} was not found.");

        // 2. Is the student allowed to borrow?
        if (!student.IsAllowedToBorrow)
            return BorrowResult.Failed($"Student '{student.Name}' is not currently allowed to borrow equipment.");

        // 3. Does the equipment exist?
        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is null)
            return BorrowResult.Failed($"Equipment {equipmentId} was not found.");

        // 4. Is the equipment currently available?
        if (!equipment.IsAvailable)
            return BorrowResult.Failed($"Equipment '{equipment.Name}' is currently unavailable.");

        // 5. Has the student reached the allowed number of active borrowings?
        var activeCount = await _borrowingRepository.CountActiveByStudentAsync(studentId, cancellationToken);
        if (activeCount >= _maxActiveBorrowingsPerStudent)
            return BorrowResult.Failed(
                $"Student '{student.Name}' already has the maximum of {_maxActiveBorrowingsPerStudent} active borrowings.");

        // 6. All rules satisfied — create and persist the borrowing.
        var borrowing = new Borrowing(
            id: GenerateBorrowingId(),
            studentId: student.Id,
            equipmentId: equipment.Id,
            dateBorrowed: DateOnly.FromDateTime(DateTime.UtcNow),
            expectedReturnDate: expectedReturnDate);

        equipment.MarkUnavailable();

        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
        await _borrowingRepository.AddAsync(borrowing, cancellationToken);

        return BorrowResult.Succeeded(borrowing);
    }

    // Simple id generation for this in-memory-only lab activity.
    // A real implementation would let the persistence layer assign ids.
    private static int GenerateBorrowingId() => Random.Shared.Next(1000, 999_999);
}
