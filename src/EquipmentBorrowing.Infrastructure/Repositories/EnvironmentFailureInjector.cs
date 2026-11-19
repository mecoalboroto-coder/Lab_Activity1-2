using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EnvironmentFailureInjector : IFailureInjector
{
    public const string VariableName = "EQUIPMENT_BORROWING_FAIL_BEFORE_COMMIT";

    public void BeforeCommit()
    {
        if (Environment.GetEnvironmentVariable(VariableName) == "1")
            throw new InjectedFailureException();
    }
}
