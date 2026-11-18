using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students", t =>
        {
            t.HasCheckConstraint("CK_Students_StudentNumber_NotBlank", "btrim(\"StudentNumber\") <> ''");
            t.HasCheckConstraint("CK_Students_Name_NotBlank", "btrim(\"Name\") <> ''");
            t.HasCheckConstraint("CK_Students_Contact_NotBlank", "\"Contact\" IS NULL OR btrim(\"Contact\") <> ''");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.StudentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Contact)
            .HasMaxLength(200);

        builder.Property(s => s.IsAllowedToBorrow)
            .IsRequired();

        builder.HasIndex(s => s.Name);
    }
}