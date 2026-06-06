using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManicureStudio.Infrastructure.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.TotalPrice)
                .HasColumnType("decimal(10,2)");

            builder.Property(a => a.ClientComment)
                .HasMaxLength(500);

            // Связь с клиентом
            builder.HasOne(a => a.Client)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь с мастером
            builder.HasOne(a => a.Master)
                .WithMany(m => m.Appointments)
                .HasForeignKey(a => a.MasterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Индекс для быстрой выборки записей по дате и мастеру
            builder.HasIndex(a => new { a.MasterId, a.StartTime })
                .HasDatabaseName("IX_Appointments_Master_StartTime");

            // Индекс для поиска записей клиента
            builder.HasIndex(a => a.ClientId)
                .HasDatabaseName("IX_Appointments_ClientId");
        }
    }
}
