namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public bool IsAvailable { get; private set; }

    public ICollection<Borrowing> Borrowings { get; private set; } = new List<Borrowing>();

    public Equipment(int id, string name, string? description = null, bool isAvailable = true)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Equipment id must be positive.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Equipment name is required.", nameof(name));

        Id = id;
        Name = name;
        Description = description;
        IsAvailable = isAvailable;
    }

    public void MarkUnavailable() => IsAvailable = false;

    public void MarkAvailable() => IsAvailable = true;
}