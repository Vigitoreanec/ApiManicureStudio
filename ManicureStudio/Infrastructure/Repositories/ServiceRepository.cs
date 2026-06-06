using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Interfaces;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ManicureStudio.Infrastructure.Repositories
{
    public class ServiceRepository(AppDbContext context) : Repository<Service>(context), IServiceRepository
    {
        public async Task<IEnumerable<Service>> GetActiveServicesWithCategoriesAsync()
        {
            return await _dbSet
                .Where(s => s.IsActive)
                .Include(s => s.Category)
                .OrderBy(s => s.Category!.SortOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }
        public async Task<IEnumerable<Service>> GetByCategoryAsync(int categoryId)
        {
            return await _dbSet
                .Where(s => s.CategoryId == categoryId && s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}
