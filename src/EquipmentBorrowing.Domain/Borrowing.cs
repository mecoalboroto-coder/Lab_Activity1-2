namespace EquipmentBorrowing.Domain;

/// <summary>
/// Represents a single borrowing transaction linking a student to a
/// piece of equipment. It owns the transition of its own status
/// (Active -> Returned), but it does NOT validate whether the
/// borrowing was allowed to happen in the first place — that
/// cross-entity validation belongs in the application service.
/// </summary>
public class Borrowing
{
    public int Id { get; }
    public int StudentId { get; }
    public int EquipmentId { get; }
    public DateOnly DateBorrowed { get; }
    public DateOnly ExpectedReturnDate { get; }
    public BorrowingStatus Status { get; private set; }

    public Borrowing(
        int id,
        int studentId,
        int equipmentId,
        DateOnly dateBorrowed,
        DateOnly expectedReturnDate)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Borrowing id must be positive.");

        if (expectedReturnDate < dateBorrowed)
            throw new ArgumentException(
                "Expected return date cannot be before the date borrowed.",
                nameof(expectedReturnDate));

        Id = id;
        StudentId = studentId;
        EquipmentId = equipmentId;
        DateBorrowed = dateBorrowed;
        ExpectedReturnDate = expectedReturnDate;
        Status = BorrowingStatus.Active;
    }

    /// <summary>
    /// Marks this borrowing as returned. Only concerns the record's own
    /// state — it does not update the related Equipment's availability;
    /// that coordination belongs to the application service, which has
    /// access to both objects.
    /// </summary>
    public void MarkReturned()
    {
        if (Status == BorrowingStatus.Returned)
            throw new InvalidOperationException("Borrowing has already been returned.");

        Status = BorrowingStatus.Returned;
    }
}
