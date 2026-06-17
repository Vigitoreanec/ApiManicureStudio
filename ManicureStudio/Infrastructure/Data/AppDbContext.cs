using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ManicureStudio.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        // ──────────────── Таблицы (DbSet) ────────────────
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Master> Masters { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<MasterService> MasterServices { get; set; } = null!;
        public DbSet<AppointmentService> AppointmentServices { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Все запросы автоматически исключают записи с IsDeleted = true
            modelBuilder.Entity<Client>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Master>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Service>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ServiceCategory>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        // При создании — фиксируем время создания
                        entry.Entity.CreatedAt = DateTime.Now;
                        break;

                    case EntityState.Modified:
                        // При обновлении — фиксируем время изменения
                        entry.Entity.UpdatedAt = DateTime.Now;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
