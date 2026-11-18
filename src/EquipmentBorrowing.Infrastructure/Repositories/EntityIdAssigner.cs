using System.Reflection;

namespace EquipmentBorrowing.Infrastructure.Repositories;

internal static class EntityIdAssigner
{
    public static void Assign(object entity, int id) =>
        entity.GetType()
            .GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(entity, id);
}
