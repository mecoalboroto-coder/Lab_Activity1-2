using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class StudentManagementService
{
    private const string FailedMessage = "The database could not be reached or the operation failed. No changes were saved.";

    private const string UnknownMessage =
        "The connection was lost while saving, so the result is unknown. " +
        "The request was not retried. Refresh and check the student record before trying again.";

    private readonly IStudentRepository _students;

    public StudentManagementService(IStudentRepository students)
    {
        _students = students;
    }

    public async Task<OperationResult<Student>> RegisterAsync(
        string? studentNumber,
        string? name,
        string? contact,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var student = new Student(studentNumber ?? string.Empty, name ?? string.Empty, contact);

            if (await _students.NumberExistsAsync(student.StudentNumber, null, cancellationToken))
                return OperationResult<Student>.Failure(DuplicateMessage(student.StudentNumber));

            await _students.AddAsync(student, cancellationToken);
            return OperationResult<Student>.Ok(student, $"Registered student '{student.Name}'.");
        }
        catch (ArgumentException ex)
        {
            return OperationResult<Student>.Validation(ex);
        }
        catch (DuplicateValueException ex)
        {
            return OperationResult<Student>.Failure(ex.Message);
        }
        catch (DataAccessException ex)
        {
            return ex.OutcomeUnknown
                ? OperationResult<Student>.Unknown(UnknownMessage)
                : OperationResult<Student>.Failure(FailedMessage);
        }
        catch (Exception)
        {
            return OperationResult<Student>.Failure(FailedMessage);
        }
    }

    public async Task<OperationResult<Student>> UpdateAsync(
        int id,
        string? studentNumber,
        string? name,
        string? contact,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = Student.NormalizeStudentNumber(studentNumber);

            if (await _students.NumberExistsAsync(normalized, id, cancellationToken))
                return OperationResult<Student>.Failure(DuplicateMessage(normalized));

            var found = await _students.UpdateDetailsAsync(
                id, studentNumber ?? string.Empty, name ?? string.Empty, contact, cancellationToken);

            if (!found)
                return OperationResult<Student>.Failure("That student no longer exists. The list has been refreshed.");

            var saved = await _students.GetByIdAsync(id, cancellationToken);
            return saved is null
                ? OperationResult<Student>.Failure("That student no longer exists. The list has been refreshed.")
                : OperationResult<Student>.Ok(saved, $"Updated student '{saved.Name}'.");
        }
        catch (ArgumentException ex)
        {
            return OperationResult<Student>.Validation(ex);
        }
        catch (DuplicateValueException ex)
        {
            return OperationResult<Student>.Failure(ex.Message);
        }
        catch (DataAccessException ex)
        {
            return ex.OutcomeUnknown
                ? OperationResult<Student>.Unknown(UnknownMessage)
                : OperationResult<Student>.Failure(FailedMessage);
        }
        catch (Exception)
        {
            return OperationResult<Student>.Failure(FailedMessage);
        }
    }

    public async Task<OperationResult<Student>> SetEligibilityAsync(
        int id,
        bool allowed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var change = await _students.SetEligibilityAsync(id, allowed, cancellationToken);

            if (change == EligibilityChange.NotFound)
                return OperationResult<Student>.Failure("That student no longer exists. The list has been refreshed.");

            var student = await _students.GetByIdAsync(id, cancellationToken);
            if (student is null)
                return OperationResult<Student>.Failure("That student no longer exists. The list has been refreshed.");

            var state = allowed ? "permitted to borrow" : "restricted from borrowing";

            return change == EligibilityChange.Unchanged
                ? OperationResult<Student>.Ok(student, $"'{student.Name}' is already {state}. Nothing was changed.")
                : OperationResult<Student>.Ok(student, $"'{student.Name}' is now {state}.");
        }
        catch (DataAccessException ex)
        {
            return ex.OutcomeUnknown
                ? OperationResult<Student>.Unknown(UnknownMessage)
                : OperationResult<Student>.Failure(FailedMessage);
        }
        catch (Exception)
        {
            return OperationResult<Student>.Failure(FailedMessage);
        }
    }

    private static string DuplicateMessage(string number) =>
        $"A student with number '{number}' already exists (numbers are compared ignoring case and surrounding spaces). No changes were made.";
}