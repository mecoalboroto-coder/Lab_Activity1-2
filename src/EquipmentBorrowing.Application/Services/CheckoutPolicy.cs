using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed record PolicyViolation(FailureKind Kind, string Message);

public static class CheckoutPolicy
{
    public const int MaxActiveBorrowings = 3;

    public static PolicyViolation? Validate(
        Student? student,
        IReadOnlyList<int> equipmentIds,
        IReadOnlyList<Equipment> equipment,
        int activeCount,
        DateOnly checkoutDate,
        DateOnly expectedReturnDate)
    {
        if (equipmentIds.Count == 0)
            return new PolicyViolation(FailureKind.Invalid, "Select at least one item.");

        if (equipmentIds.Distinct().Count() != equipmentIds.Count)
            return new PolicyViolation(FailureKind.Invalid, "The same item was selected more than once.");

        if (expectedReturnDate <= checkoutDate)
            return new PolicyViolation(FailureKind.Invalid, "The expected return date must be later than the checkout date.");

        if (student is null)
            return new PolicyViolation(FailureKind.NotFound, "The selected student no longer exists.");

        var missing = equipmentIds.Except(equipment.Select(e => e.Id)).ToList();
        if (missing.Count > 0)
            return new PolicyViolation(FailureKind.NotFound, $"Equipment not found: {string.Join(", ", missing)}.");

        if (!student.IsAllowedToBorrow)
            return new PolicyViolation(FailureKind.Conflict, $"Student '{student.Name}' is not currently allowed to borrow equipment.");

        var unavailable = equipment.Where(e => !e.IsAvailable).Select(e => e.Name).ToList();
        if (unavailable.Count > 0)
            return new PolicyViolation(FailureKind.Conflict, $"Unavailable equipment: {string.Join(", ", unavailable)}.");

        if (activeCount + equipmentIds.Count > MaxActiveBorrowings)
            return new PolicyViolation(
                FailureKind.Conflict,
                $"Student '{student.Name}' has {activeCount} active borrowing(s); " +
                $"adding {equipmentIds.Count} would exceed the limit of {MaxActiveBorrowings}.");

        return null;
    }
}
