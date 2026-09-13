using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

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

    public async Task<ReturnResult> ReturnAsync(
        int borrowingId,
        CancellationToken cancellationToken = default)
    {
        var borrowing = await _borrowingRepository.GetByIdAsync(borrowingId, cancellationToken);

        if (borrowing is null)
            return ReturnResult.Failed($"Borrowing {borrowingId} was not found.");

        if (borrowing.Status == Domain.BorrowingStatus.Returned)
            return ReturnResult.Failed("This borrowing has already been returned.");

        borrowing.MarkReturned();
        await _borrowingRepository.UpdateAsync(borrowing, cancellationToken);

        var equipment = await _equipmentRepository.GetByIdAsync(borrowing.EquipmentId, cancellationToken);
        if (equipment is not null)
        {
            equipment.MarkAvailable();
            await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
        }

        return ReturnResult.Succeeded(borrowing);
    }
}