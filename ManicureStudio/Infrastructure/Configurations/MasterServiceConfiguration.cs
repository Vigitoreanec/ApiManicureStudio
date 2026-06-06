using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManicureStudio.Infrastructure.Configurations
{
    public class MasterServiceConfiguration : IEntityTypeConfiguration<MasterService>
    {
        public void Configure(EntityTypeBuilder<MasterService> builder)
        {
            builder.ToTable("MasterServices");

            // Составной первичный ключ
            builder.HasKey(ms => new { ms.MasterId, ms.ServiceId });

            builder.Property(ms => ms.CustomPrice)
                .HasColumnType("decimal(10,2)");

            builder.HasOne(ms => ms.Master)
                .WithMany(m => m.MasterServices)
                .HasForeignKey(ms => ms.MasterId);

            builder.HasOne(ms => ms.Service)
                .WithMany(s => s.MasterServices)
                .HasForeignKey(ms => ms.ServiceId);
        }
    }
}
