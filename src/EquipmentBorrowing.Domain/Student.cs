namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; private set; }
    public string StudentNumber { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Contact { get; private set; }
    public bool IsAllowedToBorrow { get; private set; }

    public ICollection<Borrowing> Borrowings { get; private set; } = new List<Borrowing>();

    private Student()
    {
    }

    public Student(string studentNumber, string name, string? contact = null)
    {
        ApplyDetails(studentNumber, name, contact);
        IsAllowedToBorrow = true;
    }

    public void UpdateDetails(string studentNumber, string name, string? contact) =>
        ApplyDetails(studentNumber, name, contact);

    public void Suspend() => IsAllowedToBorrow = false;

    public void Reinstate() => IsAllowedToBorrow = true;

    public static string NormalizeStudentNumber(string? value) => (value ?? string.Empty).Trim();

    private void ApplyDetails(string studentNumber, string name, string? contact)
    {
        var normalizedNumber = NormalizeStudentNumber(studentNumber);
        var trimmedName = (name ?? string.Empty).Trim();

        if (normalizedNumber.Length == 0)
            throw new ArgumentException("Student number is required.", nameof(studentNumber));

        if (trimmedName.Length == 0)
            throw new ArgumentException("Student name is required.", nameof(name));

        StudentNumber = normalizedNumber;
        Name = trimmedName;
        Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();
    }
}