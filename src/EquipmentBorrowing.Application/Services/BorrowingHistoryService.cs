using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class BorrowingHistoryService
{
    private const string FailedMessage =
        "The borrowing records could not be loaded. Check that PostgreSQL is running, then try again.";

    private readonly IStudentRepository _students;
    private readonly IEquipmentRepository _equipment;
    private readonly IBorrowingQueries _queries;

    public BorrowingHistoryService(
        IStudentRepository students,
        IEquipmentRepository equipment,
        IBorrowingQueries queries)
    {
        _students = students;
        _equipment = equipment;
        _queries = queries;
    }

    public async Task<QueryResult<Borrowing>> ForStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var student = await _students.GetByIdAsync(studentId, cancellationToken);
            if (student is null)
                return QueryResult<Borrowing>.Missing("That student no longer exists. Please select another student.");

            return QueryResult<Borrowing>.Loaded(await _queries.GetByStudentAsync(studentId, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return QueryResult<Borrowing>.Failed(FailedMessage);
        }
    }

    public async Task<QueryResult<Borrowing>> ForEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _equipment.GetByIdAsync(equipmentId, cancellationToken);
            if (item is null)
                return QueryResult<Borrowing>.Missing("That item no longer exists. Please select another item.");

            return QueryResult<Borrowing>.Loaded(await _queries.GetByEquipmentAsync(equipmentId, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return QueryResult<Borrowing>.Failed(FailedMessage);
        }
    }

    public async Task<QueryResult<Borrowing>> SearchAsync(BorrowingSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        try
        {
            return QueryResult<Borrowing>.Loaded(await _queries.SearchAsync(criteria, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return QueryResult<Borrowing>.Failed(FailedMessage);
        }
    }
}
