using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.ToTable("Borrowings", t =>
        {
            t.HasCheckConstraint(
                "CK_Borrowings_ExpectedReturn_After_Borrowed",
                "\"ExpectedReturnDate\" > \"DateBorrowed\"");

            t.HasCheckConstraint(
                "CK_Borrowings_Status_ReturnedAt_Consistent",
                "(\"Status\" = 'Active' AND \"ReturnedAt\" IS NULL) OR " +
                "(\"Status\" = 'Returned' AND \"ReturnedAt\" IS NOT NULL AND \"ReturnedAt\" >= \"DateBorrowed\")");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd();

        builder.Property(b => b.StudentId)
            .IsRequired();

        builder.Property(b => b.EquipmentId)
            .IsRequired();

        builder.Property(b => b.DateBorrowed)
            .IsRequired();

        builder.Property(b => b.ExpectedReturnDate)
            .IsRequired();

        builder.Property(b => b.ReturnedAt)
            .IsRequired(false);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(b => b.Student)
            .WithMany(s => s.Borrowings)
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Equipment)
            .WithMany(e => e.Borrowings)
            .HasForeignKey(b => b.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.StudentId);
        builder.HasIndex(b => b.Status);

        builder.HasIndex(b => b.EquipmentId)
            .IsUnique()
            .HasDatabaseName("UX_Borrowings_EquipmentId_Active")
            .HasFilter("\"Status\" = 'Active'");
    }
}