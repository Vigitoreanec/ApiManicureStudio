using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManicureStudio.Infrastructure.Configurations
{
    public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
    {
        public void Configure(EntityTypeBuilder<ServiceCategory> builder)
        {
            builder.ToTable("ServiceCategories");

            builder.HasKey(sc => sc.Id);

            builder.Property(sc => sc.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sc => sc.Description)
                .HasMaxLength(500);
        }
    }
}
