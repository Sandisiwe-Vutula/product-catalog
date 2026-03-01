using Microsoft.EntityFrameworkCore;
using ProductCatalog.API.Data;
using ProductCatalog.API.Interfaces;

namespace ProductCatalog.API.Repositories
{
    /// <summary>
    /// Generic base repository over EF Core.
    /// </summary>
    public abstract class RepositoryBase<T, TKey> : IRepository<T, TKey>
        where T : class
        where TKey : struct
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        protected RepositoryBase(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync() =>
            await _dbSet.ToListAsync();

        public virtual async Task<T?> GetByIdAsync(TKey id) =>
            await _dbSet.FindAsync(id);

        public virtual async Task<T> AddAsync(T entity)
        {
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(TKey id)
        {
            var entity = await GetByIdAsync(id);
            if (entity is null) return false;
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> ExistsAsync(TKey id) =>
            await _dbSet.FindAsync(id) is not null;
    }
}
