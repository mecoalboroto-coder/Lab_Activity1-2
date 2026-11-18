namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; private set; }
    public string Identifier { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; }

    public ICollection<Borrowing> Borrowings { get; private set; } = new List<Borrowing>();

    private Equipment()
    {
    }

    public Equipment(string identifier, string name, string? description = null)
    {
        ApplyDetails(identifier, name, description);
        IsAvailable = true;
    }

    public void UpdateDetails(string identifier, string name, string? description) =>
        ApplyDetails(identifier, name, description);

    public void MarkUnavailable() => IsAvailable = false;

    public void MarkAvailable() => IsAvailable = true;

    public static string NormalizeIdentifier(string? value) => (value ?? string.Empty).Trim();

    private void ApplyDetails(string identifier, string name, string? description)
    {
        var normalizedIdentifier = NormalizeIdentifier(identifier);
        var trimmedName = (name ?? string.Empty).Trim();

        if (normalizedIdentifier.Length == 0)
            throw new ArgumentException("Equipment identifier is required.", nameof(identifier));

        if (trimmedName.Length == 0)
            throw new ArgumentException("Equipment name is required.", nameof(name));

        Identifier = normalizedIdentifier;
        Name = trimmedName;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}