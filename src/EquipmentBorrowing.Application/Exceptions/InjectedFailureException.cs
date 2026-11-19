namespace EquipmentBorrowing.Application.Exceptions;

public class InjectedFailureException : InvalidOperationException
{
    public InjectedFailureException() : base("A controlled test failure was injected before commit.")
    {
    }
}
