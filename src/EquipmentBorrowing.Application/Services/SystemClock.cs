using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

public sealed class FixedClock : IClock
{
    public FixedClock(DateOnly today)
    {
        Today = today;
    }

    public DateOnly Today { get; }
}
