namespace EquipmentBorrowing.Domain;

/// <summary>
/// Represents a student who may borrow equipment.
/// Holds only information intrinsic to the student — it does not know
/// how many items it currently has borrowed; that is derived from
/// <see cref="Borrowing"/> records by the application layer.
/// </summary>
public class Student
{
    public int Id { get; }
    public string Name { get; }

    /// <summary>
    /// Whether the student is currently in good standing and eligible
    /// to borrow equipment at all (independent of any specific request).
    /// </summary>
    public bool IsAllowedToBorrow { get; private set; }

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
