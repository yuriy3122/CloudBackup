using System.Linq.Expressions;
using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public interface IRepository<TEntity> : IDisposable where TEntity : IEntity
    {
        Task<TEntity> FindByIdAsync(int id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes);

        Task<IEnumerable<TEntity>> FindByIdsAsync(ICollection<int> ids, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes);

        Task<IEnumerable<TEntity>> FindAllAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes);

        Task<IEnumerable<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> filter, string? orderBy, EntitiesPage? page,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes,
            bool enableTracking = false);

        Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes);

        Task<int> CountDistinctAsync<TResult>(
            Expression<Func<TEntity, TResult>> property,
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes);

        void Add(TEntity entity);

        void AddRange(IEnumerable<TEntity> entities);

        void Update(TEntity entity);

        void Remove(TEntity entity);

        Task SaveChangesAsync(bool resolveConcurrenceConflict = true);
    }
}