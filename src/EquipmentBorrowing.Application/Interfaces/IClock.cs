namespace EquipmentBorrowing.Application.Interfaces;

public interface IClock
{
    DateOnly Today { get; }
}
