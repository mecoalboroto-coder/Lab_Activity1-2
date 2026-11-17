namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; }
    public int StudentId { get; }
    public int EquipmentId { get; }
    public DateOnly DateBorrowed { get; }
    public DateOnly ExpectedReturnDate { get; }
    public DateOnly? ReturnedAt { get; private set; }
    public BorrowingStatus Status { get; private set; }

    public Student? Student { get; private set; }
    public Equipment? Equipment { get; private set; }

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

    public void MarkReturned(DateOnly returnedAt)
    {
        if (Status == BorrowingStatus.Returned)
            throw new InvalidOperationException("Borrowing has already been returned.");

        Status = BorrowingStatus.Returned;
        ReturnedAt = returnedAt;
    }
}