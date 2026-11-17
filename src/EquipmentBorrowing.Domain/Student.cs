namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; }
    public string Name { get; }
    public bool IsAllowedToBorrow { get; private set; }

    public ICollection<Borrowing> Borrowings { get; private set; } = new List<Borrowing>();

    public Student(int id, string name, bool isAllowedToBorrow = true)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Student id must be positive.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Student name is required.", nameof(name));

        Id = id;
        Name = name;
        IsAllowedToBorrow = isAllowedToBorrow;
    }

    public void Suspend() => IsAllowedToBorrow = false;

    public void Reinstate() => IsAllowedToBorrow = true;
}