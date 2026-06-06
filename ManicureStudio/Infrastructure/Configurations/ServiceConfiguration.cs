using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManicureStudio.Infrastructure.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("Services");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Description)
                .HasMaxLength(1000);

            // Цена: до 999999.99 рублей
            builder.Property(s => s.Price)
                .HasColumnType("decimal(10,2)");

            // Связь с категорией
            builder.HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить категорию, у которой есть услуги

            builder.HasIndex(s => s.CategoryId)
                .HasDatabaseName("IX_Services_CategoryId");
        }
    }
}
