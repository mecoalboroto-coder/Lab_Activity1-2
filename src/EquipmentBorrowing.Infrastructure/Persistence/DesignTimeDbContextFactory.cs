using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>();
        optionsBuilder.UseSqlite("Data Source=equipmentborrowing.db");

        return new EquipmentBorrowingDbContext(optionsBuilder.Options);
    }
}