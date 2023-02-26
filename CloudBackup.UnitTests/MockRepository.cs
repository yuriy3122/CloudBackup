using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.UnitTests
{
    public class MockRepository<T> : IRepository<T> where T : IEntity
    {
        private readonly List<T> _collection;

        public MockRepository()
        {
            _collection = new List<T>();
        }

        public MockRepository(IEnumerable<T> collection)
        {
            _collection = collection.ToList();
        }

        public void ReplaceItems(IEnumerable<T> collection)
        {
            _collection.Clear();
            _collection.AddRange(collection);
        }

        public Task<T> FindByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? includes = null)
        {
            var entity = _collection.First(x => x.Id == id);

            if (entity == null)
            {
                string message = nameof(entity);
                throw new ArgumentNullException(message);
            }

            return Task.Run(() => entity);
        }

        public Task<IEnumerable<T>> FindByIdsAsync(ICollection<int> ids, Func<IQueryable<T>, IQueryable<T>>? includes = null)
        {
            return Task.Run(() => _collection.Where(x => ids.Contains(x.Id)));
        }

        public Task<IEnumerable<T>> FindAllAsync(Func<IQueryable<T>, IQueryable<T>>? includes = null)
        {
            return Task.Run(() => _collection.AsEnumerable());
        }

        public Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> filter, string? orderBy, EntitiesPage? page = null,
            Func<IQueryable<T>, IQueryable<T>>? includes = null,
            bool enableTracking = false)
        {
            var query = _collection.AsQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                query = query.OrderBy(orderBy);
            }

            if (page != null)
            {
                var skip = (page.CurrentPage - 1) * page.NumberOfEntitiesOnPage;
                query = query.Skip(skip).Take(page.NumberOfEntitiesOnPage);
            }

            return Task.Run(() => query.AsEnumerable());
        }

        public Task<int> CountAsync(Expression<Func<T, bool>>? filter, Func<IQueryable<T>, IQueryable<T>>? includes = null)
        {
            var query = _collection.AsEnumerable();

            if (filter != null)
            {
                query = query.Where(filter.Compile());
            }

            return Task.Run(() => query.Count());
        }

        public Task<int> CountDistinctAsync<TResult>(Expression<Func<T, TResult>> property, Expression<Func<T, bool>> filter, Func<IQueryable<T>, IQueryable<T>>? includes = null)
        {
            var query = _collection.AsEnumerable();

            if (filter != null)
            {
                query = query.Where(filter.Compile());
            }

            return Task.Run(() => query.Select(property.Compile()).Distinct().Count());
        }

        public void Add(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _collection.Add(entity);
        }

        public void AddRange(IEnumerable<T> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            _collection.AddRange(entities);
        }

        public void Update(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var index = _collection.FindIndex(x => x.Id == entity.Id);

            if (index == -1)
            {
                throw new ArgumentException($"Entity with id={entity.Id} not found.", nameof(entity));
            }

            _collection[index] = entity;
        }

        public void Remove(T entity)
        {
            if (entity == null)

                throw new ArgumentNullException(nameof(entity));

            _collection.RemoveAll(x => x.Id == entity.Id);
        }

        public Task RemoveAllAsync()
        {
            return Task.Run(() => _collection.Clear());
        }

        public Task SaveChangesAsync(bool resolveConcurrenceConflict = true)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
