namespace EquipmentBorrowing.Application.Exceptions;

public class DataAccessException : Exception
{
    public bool OutcomeUnknown { get; }

    public DataAccessException(string message, bool outcomeUnknown, Exception? inner = null)
        : base(message, inner)
    {
        OutcomeUnknown = outcomeUnknown;
    }
}