using EquipmentBorrowing.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EquipmentBorrowing.Infrastructure.Repositories;

internal static class PersistenceErrors
{
    public static async Task SaveAsync(DbContext context, string duplicateMessage, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateValueException(duplicateMessage, ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException)
        {
            throw new DataAccessException("The database rejected the change. Nothing was saved.", false, ex);
        }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException or IOException or TimeoutException or InvalidOperationException)
        {
            throw new DataAccessException("The connection failed while saving.", true, ex);
        }
    }
}