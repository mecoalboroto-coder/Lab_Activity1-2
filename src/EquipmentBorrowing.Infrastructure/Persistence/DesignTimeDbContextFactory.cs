using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    private const string UserSecretsId = "daad92a0-38e7-4366-8395-e1d073090c8e";

    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(UserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:Default' is not configured. See the README.");

        var optionsBuilder = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new EquipmentBorrowingDbContext(optionsBuilder.Options);
    }
}