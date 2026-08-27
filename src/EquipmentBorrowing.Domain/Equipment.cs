namespace EquipmentBorrowing.Domain;

/// <summary>
/// Represents a piece of equipment that can be borrowed.
/// It knows only its own availability; it does not decide whether a
/// particular student is allowed to borrow it — that cross-entity rule
/// belongs to the application service.
/// </summary>
public class Equipment
{
    public int Id { get; }
    public string Name { get; }
    public bool IsAvailable { get; private set; }

    public Equipment(int id, string name, bool isAvailable = true)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Equipment id must be positive.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Equipment name is required.", nameof(name));

        Id = id;
        Name = name;
        IsAvailable = isAvailable;
    }

    public void MarkUnavailable() => IsAvailable = false;

    public void MarkAvailable() => IsAvailable = true;
}
