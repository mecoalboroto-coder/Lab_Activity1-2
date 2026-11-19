using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class EquipmentManagementService
{
    private const string FailedMessage = "The database could not be reached or the operation failed. No changes were saved.";

    private const string UnknownMessage =
        "The connection was lost while saving, so the result is unknown. " +
        "The request was not retried. Refresh and check the equipment record before trying again.";

    private readonly IEquipmentRepository _equipment;

    public EquipmentManagementService(IEquipmentRepository equipment)
    {
        _equipment = equipment;
    }

    public async Task<OperationResult<Equipment>> RegisterAsync(
        string? identifier,
        string? name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var item = new Equipment(identifier ?? string.Empty, name ?? string.Empty, description);

            if (await _equipment.IdentifierExistsAsync(item.Identifier, null, cancellationToken))
                return OperationResult<Equipment>.Failure(DuplicateMessage(item.Identifier));

            await _equipment.AddAsync(item, cancellationToken);
            return OperationResult<Equipment>.Ok(item, $"Registered equipment '{item.Name}' as available.");
        }
        catch (ArgumentException ex)
        {
            return OperationResult<Equipment>.Validation(ex);
        }
        catch (DuplicateValueException ex)
        {
            return OperationResult<Equipment>.Failure(ex.Message);
        }
        catch (DataAccessException ex)
        {
            return ex.OutcomeUnknown
                ? OperationResult<Equipment>.Unknown(UnknownMessage)
                : OperationResult<Equipment>.Failure(FailedMessage);
        }
        catch (Exception)
        {
            return OperationResult<Equipment>.Failure(FailedMessage);
        }
    }

    public async Task<OperationResult<Equipment>> UpdateAsync(
        int id,
        string? identifier,
        string? name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = Equipment.NormalizeIdentifier(identifier);

            if (await _equipment.IdentifierExistsAsync(normalized, id, cancellationToken))
                return OperationResult<Equipment>.Failure(DuplicateMessage(normalized));

            var found = await _equipment.UpdateDetailsAsync(
                id, identifier ?? string.Empty, name ?? string.Empty, description, cancellationToken);

            if (!found)
                return OperationResult<Equipment>.Failure("That item no longer exists. The list has been refreshed.");

            var saved = await _equipment.GetByIdAsync(id, cancellationToken);
            return saved is null
                ? OperationResult<Equipment>.Failure("That item no longer exists. The list has been refreshed.")
                : OperationResult<Equipment>.Ok(saved, $"Updated equipment '{saved.Name}'.");
        }
        catch (ArgumentException ex)
        {
            return OperationResult<Equipment>.Validation(ex);
        }
        catch (DuplicateValueException ex)
        {
            return OperationResult<Equipment>.Failure(ex.Message);
        }
        catch (DataAccessException ex)
        {
            return ex.OutcomeUnknown
                ? OperationResult<Equipment>.Unknown(UnknownMessage)
                : OperationResult<Equipment>.Failure(FailedMessage);
        }
        catch (Exception)
        {
            return OperationResult<Equipment>.Failure(FailedMessage);
        }
    }

    private static string DuplicateMessage(string identifier) =>
        $"Equipment with identifier '{identifier}' already exists (identifiers are compared ignoring case and surrounding spaces). No changes were made.";
}