using System.Reflection;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.JobProcessorApp
{
    public interface IRepositoryFactory
    {
        IRepository<TEntity> GetRepository<TEntity>() where TEntity : IEntity;
    }

    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly DbContextOptions<BackupContext> _options;

        public RepositoryFactory(DbContextOptions<BackupContext> options)
        {
            _options = options;
        }

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : IEntity
        {
            var repositoryInterface = typeof(IRepository<TEntity>);

            var assembly = Assembly.GetAssembly(typeof(BackupContext));

            if (assembly == null)
            {
                throw new ArgumentException($"Can't find Context class");
            }

            var repositoryType = assembly.ExportedTypes.FirstOrDefault(type => type.IsClass && type.GetInterfaces().Contains(repositoryInterface));

            if (repositoryType == null)
            {
                throw new ArgumentException($"Can't find repository for '{repositoryInterface}'");
            }

            var context = new BackupContext(_options);

            var repository = Activator.CreateInstance(repositoryType, context);

            return (IRepository<TEntity>)repository!;
        }
    }
}
