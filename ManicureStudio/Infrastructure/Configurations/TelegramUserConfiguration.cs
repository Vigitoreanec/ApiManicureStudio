using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManicureStudio.Infrastructure.Configurations
{
    public class TelegramUserConfiguration : IEntityTypeConfiguration<TelegramUser>
    {
        public void Configure(EntityTypeBuilder<TelegramUser> builder)
        {
            builder.ToTable("TelegramUsers");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.TelegramId)
               .IsRequired();

            builder.Property(t => t.Username)
                .HasMaxLength(100);

            builder.Property(t => t.FirstName)
                .HasMaxLength(100);

            builder.Property(t => t.LastName)
                .HasMaxLength(100);

            builder.Property(t => t.ClientId);
            builder.Property(t => t.MasterId);

            builder.Property(t => t.CurrentStep)
                .HasMaxLength(50);

            builder.Property(t => t.SessionData)
                .HasMaxLength(4000);

            builder.Property(t => t.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(t => t.TelegramId)
               .IsUnique()
               .HasDatabaseName("IX_TelegramUsers_TelegramId");

            builder.HasIndex(t => new { t.IsDeleted, t.UpdatedAt })
                .HasDatabaseName("IX_TelegramUsers_IsDeleted_UpdatedAt");

            builder.HasIndex(t => t.ClientId)
                .HasDatabaseName("IX_TelegramUsers_ClientId");

            builder.HasIndex(t => t.MasterId)
                .HasDatabaseName("IX_TelegramUsers_MasterId");



            builder.HasOne(t => t.Client)
                .WithMany() 
                .HasForeignKey(t => t.ClientId)
                .OnDelete(DeleteBehavior.SetNull) // При удалении клиента - NULL
                .HasConstraintName("FK_TelegramUsers_ClientId");

            builder.HasOne(t => t.Master)
                .WithMany() 
                .HasForeignKey(t => t.MasterId)
                .OnDelete(DeleteBehavior.SetNull) // При удалении мастера - NULL
                .HasConstraintName("FK_TelegramUsers_MasterId");
        }
    }
}
