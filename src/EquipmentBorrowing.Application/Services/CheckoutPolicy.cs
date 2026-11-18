using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public static class CheckoutPolicy
{
    public const int MaxActiveBorrowings = 3;

    public static string? Validate(
        Student? student,
        IReadOnlyList<int> equipmentIds,
        IReadOnlyList<Equipment> equipment,
        int activeCount,
        DateOnly checkoutDate,
        DateOnly expectedReturnDate)
    {
        if (equipmentIds.Count == 0)
            return "Select at least one item.";

        if (equipmentIds.Distinct().Count() != equipmentIds.Count)
            return "The same item was selected more than once.";

        if (expectedReturnDate <= checkoutDate)
            return "The expected return date must be later than the checkout date.";

        if (student is null)
            return "The selected student no longer exists.";

        if (!student.IsAllowedToBorrow)
            return $"Student '{student.Name}' is not currently allowed to borrow equipment.";

        var missing = equipmentIds.Except(equipment.Select(e => e.Id)).ToList();
        if (missing.Count > 0)
            return $"Equipment not found: {string.Join(", ", missing)}.";

        var unavailable = equipment.Where(e => !e.IsAvailable).Select(e => e.Name).ToList();
        if (unavailable.Count > 0)
            return $"Unavailable equipment: {string.Join(", ", unavailable)}.";

        if (activeCount + equipmentIds.Count > MaxActiveBorrowings)
            return $"Student '{student.Name}' has {activeCount} active borrowing(s); " +
                   $"adding {equipmentIds.Count} would exceed the limit of {MaxActiveBorrowings}.";

        return null;
    }
}