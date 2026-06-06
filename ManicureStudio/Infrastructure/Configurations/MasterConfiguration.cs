using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManicureStudio.Infrastructure.Configurations
{
    public class MasterConfiguration : IEntityTypeConfiguration<Master>
    {
        public void Configure(EntityTypeBuilder<Master> builder)
        {
            builder.ToTable("Masters");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(m => m.Specialization)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(m => m.Description)
                .HasMaxLength(2000);

            builder.Property(m => m.PhotoUrl)
                .HasMaxLength(500);

            builder.HasIndex(m => m.PhoneNumber)
                .IsUnique()
                .HasDatabaseName("IX_Masters_PhoneNumber");
        }
    }
}
