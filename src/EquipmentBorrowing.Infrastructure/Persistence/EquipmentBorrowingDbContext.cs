using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class EquipmentBorrowingDbContext : DbContext
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Borrowing> Borrowings => Set<Borrowing>();

    public EquipmentBorrowingDbContext(DbContextOptions<EquipmentBorrowingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EquipmentBorrowingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}