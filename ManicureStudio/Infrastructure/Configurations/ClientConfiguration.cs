using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManicureStudio.Infrastructure.Configurations
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.ToTable("Clients"); // Имя таблицы в БД

            builder.HasKey(c => c.Id);

            builder.Property(c => c.FirstName)
                .IsRequired()
                .HasMaxLength(100);       // Ограничение длины имени

            builder.Property(c => c.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Email)
                .HasMaxLength(200);

            builder.Property(c => c.Notes)
                .HasMaxLength(1000);

            // Уникальный индекс по телефону — нельзя создать двух клиентов с одним номером
            builder.HasIndex(c => c.PhoneNumber)
                .IsUnique()
                .HasDatabaseName("IX_Clients_PhoneNumber");

            // Индекс для быстрого поиска по email
            builder.HasIndex(c => c.Email)
                .HasDatabaseName("IX_Clients_Email");
        }
    }
}
