namespace EquipmentBorrowing.Application.Exceptions;

public class DuplicateValueException : Exception
{
    public DuplicateValueException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}