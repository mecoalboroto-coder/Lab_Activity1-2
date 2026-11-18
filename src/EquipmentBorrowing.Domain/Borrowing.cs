namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public int EquipmentId { get; private set; }
    public DateOnly DateBorrowed { get; private set; }
    public DateOnly ExpectedReturnDate { get; private set; }
    public DateOnly? ReturnedAt { get; private set; }
    public BorrowingStatus Status { get; private set; }

    public Student? Student { get; private set; }
    public Equipment? Equipment { get; private set; }

    private Borrowing()
    {
    }

    public Borrowing(
        int studentId,
        int equipmentId,
        DateOnly dateBorrowed,
        DateOnly expectedReturnDate)
    {
        if (expectedReturnDate <= dateBorrowed)
            throw new ArgumentException(
                "Expected return date must be later than the date borrowed.",
                nameof(expectedReturnDate));

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

        if (returnedAt < DateBorrowed)
            throw new ArgumentException(
                "Return date cannot precede the date borrowed.",
                nameof(returnedAt));

        Status = BorrowingStatus.Returned;
        ReturnedAt = returnedAt;
    }
}