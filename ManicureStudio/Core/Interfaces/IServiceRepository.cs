using ManicureStudio.Core.Entities;

namespace ManicureStudio.Core.Interfaces
{
    public interface IServiceRepository : IRepository<Service>
    {
        Task<IEnumerable<Service>> GetActiveServicesWithCategoriesAsync();
        Task<IEnumerable<Service>> GetByCategoryAsync(int categoryId);
    }
}
