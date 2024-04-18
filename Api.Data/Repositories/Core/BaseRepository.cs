using Api.Data.Data;
using Api.Data.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories.Core
{
    public interface IRepository<TEntity>
    {
        Task<List<TEntity>> GetAll();
        Task<List<TEntity>> Filter(int page, int pageSize, string searchPhrase);
        Task<TEntity?> FindById(int? id);
        Task<TEntity> Insert(TEntity entity);
        Task Update(TEntity entity);
        Task DeleteNonSafe(TEntity entity);
    }

    public class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly BaseApiDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public BaseRepository(BaseApiDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        public virtual async Task<List<TEntity>> GetAll()
        {
            if (typeof(TEntity).BaseType == typeof(BaseEntity))
            {
                var result = new List<TEntity>();
                foreach (var entity in _dbSet)
                {
                    if (entity is BaseEntity baseEntity && baseEntity.DateDeleted == null) result.Add(entity);
                }
                return result;
            }
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<List<TEntity>> Filter(int page, int pageSize, string searchPhrase)
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<TEntity?> FindById(int? id)
        {
            if (id == null) return null;

            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<TEntity> Insert(TEntity entity)
        {
            TEntity newEntity = (await _dbSet.AddAsync(entity)).Entity;
            await _context.SaveChangesAsync();
            return newEntity;
        }

        public async Task Update(TEntity entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task Delete(TEntity entity)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.DateDeleted = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public virtual async Task DeleteNonSafe(TEntity entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
