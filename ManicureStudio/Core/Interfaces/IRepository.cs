using ManicureStudio.Core.Entities;
using System.Linq.Expressions;

namespace ManicureStudio.Core.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>Найти записи по условию</summary>
        /// <param name="predicate">Лямбда-выражение фильтра</param>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        // ──────────────── Записи ────────────────
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task SoftDeleteAsync(int id);
        Task HardDeleteAsync(int id);
        Task<int> SaveChangesAsync();
    }
}

