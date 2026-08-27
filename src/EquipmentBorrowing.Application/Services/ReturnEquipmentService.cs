using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

/// <summary>
/// Bonus/optional: coordinates the "Return Equipment" use case so the
/// demonstration in Part H can show a full borrow -> return cycle.
/// Not required by the lab (only one use case is required), but kept
/// small and focused, following the same pattern as BorrowEquipmentService.
/// </summary>
public class ReturnEquipmentService
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;

    public ReturnEquipmentService(
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository)
    {
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
    }

    public async Task<bool> ReturnAsync(
        int studentId,
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        var borrowing = await _borrowingRepository.GetActiveByStudentAndEquipmentAsync(
            studentId, equipmentId, cancellationToken);

        if (borrowing is null)
            return false;

        borrowing.MarkReturned();
        await _borrowingRepository.UpdateAsync(borrowing, cancellationToken);

        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is not null)
        {
            equipment.MarkAvailable();
            await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
        }

        return true;
    }
}
