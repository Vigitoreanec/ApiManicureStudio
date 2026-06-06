using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManicureStudio.Infrastructure.Configurations
{
    public class AppointmentServiceConfiguration : IEntityTypeConfiguration<AppointmentService>
    {
        public void Configure(EntityTypeBuilder<AppointmentService> builder)
        {
            builder.ToTable("AppointmentServices");

            // Составной первичный ключ
            builder.HasKey(a => new { a.AppointmentId, a.ServiceId });

            builder.Property(a => a.PriceAtBooking)
                .HasColumnType("decimal(10,2)");

            builder.HasOne(a => a.Appointment)
                .WithMany(ap => ap.AppointmentServices)
                .HasForeignKey(a => a.AppointmentId);

            builder.HasOne(a => a.Service)
                .WithMany(s => s.AppointmentServices)
                .HasForeignKey(a => a.ServiceId);
        }
    }
}
