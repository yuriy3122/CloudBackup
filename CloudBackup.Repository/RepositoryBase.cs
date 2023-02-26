using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public abstract class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : EntityBase
    {
        internal BackupContext Context;

        protected RepositoryBase(BackupContext context)
        {
            Context = context;
        }

        public virtual async Task<IEnumerable<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            string? orderBy,
            EntitiesPage? page,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes,
            bool enableTracking = false)
        {
            var query = GetInitialQuery();

            if (!enableTracking)
            {
                query = query.AsNoTracking();
            }

            if (includes != null)
            {
                query = includes(query);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            query = ProcessQuery(query);

            if (!string.IsNullOrEmpty(orderBy))
            {
                query = query.OrderBy(orderBy);
            }

            if (page != null)
            {
                var skip = (page.CurrentPage - 1) * page.NumberOfEntitiesOnPage;
                query = query.Skip(skip).Take(page.NumberOfEntitiesOnPage);
            }

            return await query.ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> FindAllAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes)
        {
            var query = GetInitialQuery().AsNoTracking();

            if (includes != null)
            {
                query = includes(query);
            }

            query = ProcessQuery(query);

            return await query.ToListAsync();
        }

        public virtual async Task<TEntity> FindByIdAsync(int id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes)
        {
            var query = GetInitialQuery();

            if (includes != null)
            {
                query = includes(query);
            }

            query = ProcessQuery(query);

            return await query.FirstAsync(i => i.Id == id);
        }

        public virtual async Task<IEnumerable<TEntity>> FindByIdsAsync(ICollection<int> ids, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes)
        {
            var query = GetInitialQuery().Where(i => ids.Contains(i.Id));

            if (includes != null)
            {
                query = includes(query);
            }

            query = ProcessQuery(query);

            return await query.ToListAsync();
        }

        public virtual void Add(TEntity entity)
        {
            Context.Set<TEntity>().Add(entity);
        }

        public virtual void AddRange(IEnumerable<TEntity> entities)
        {
            Context.Set<TEntity>().AddRange(entities);
        }

        public virtual void Update(TEntity entity)
        {
            var entry = Context.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                var existingEntity = Context.Set<TEntity>().Local.FirstOrDefault(e => e.Id == entity.Id);

                if (existingEntity != null)
                {
                    Context.Entry(existingEntity).State = EntityState.Detached;
                }

                Context.Attach(entity);
                entry.State = EntityState.Modified;
            }
        }

        public virtual void Remove(TEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            var entry = Context.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                Context.Attach(entity);
            }

            Context.Set<TEntity>().Remove(entity);
        }

        public virtual async Task SaveChangesAsync(bool resolveConcurrenceConflict = true)
        {
            var doSave = true;

            while (doSave)
            {
                try
                {
                    await Context.SaveChangesAsync();
                    doSave = false;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (resolveConcurrenceConflict)
                    {
                        ResolveConcurrenceConflicts(ex, out doSave);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes)
        {
            var query = Context.Set<TEntity>().AsNoTracking().AsQueryable();

            if (includes != null)
            {
                query = includes(query);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            query = ProcessQuery(query);

            return await query.CountAsync();
        }

        public virtual async Task<int> CountDistinctAsync<TResult>(
            Expression<Func<TEntity, TResult>> property,
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes)
        {
            var query = Context.Set<TEntity>().AsNoTracking().AsQueryable();

            if (includes != null)
            {
                query = includes(query);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            query = ProcessQuery(query);

            return await query.Select(property).Distinct().CountAsync();
        }

        public void Dispose()
        {
            if (Context != null)
            {
                Context.Dispose();
            }
        }

        protected virtual IQueryable<TEntity> GetInitialQuery()
        {
            return Context.Set<TEntity>().AsQueryable();
        }

        protected virtual IQueryable<TEntity> ProcessQuery(IQueryable<TEntity> query)
        {
            return query;
        }

        private static void ResolveConcurrenceConflicts(DbUpdateConcurrencyException ex, out bool saveRequired)
        {
            saveRequired = false;

            foreach (var entry in ex.Entries)
            {
                var databaseValues = entry?.GetDatabaseValues();

                if (databaseValues == null)
                {
                    continue;
                }

                var canMerge = true;
                var mergedValues = databaseValues.Clone();
                var properties = entry?.CurrentValues.Properties;

                if (entry != null)
                {
                    if (properties != null)
                    {
                        foreach (var p in properties)
                        {
                            var databaseValue = databaseValues[p];

                            if (databaseValue == null) continue;

                            bool isModified = entry.Property(p.Name).IsModified;

                            if (isModified)
                            {
                                mergedValues[p] = entry.CurrentValues[p];

                                if (!databaseValue.Equals(entry.OriginalValues[p]) && !databaseValue.Equals(entry.CurrentValues[p]))
                                {
                                    canMerge = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (canMerge)
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                        entry.CurrentValues.SetValues(mergedValues);
                        saveRequired = true;
                    }
                    else
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                        saveRequired = true;
                    }
                }
            }
        }
    }
}