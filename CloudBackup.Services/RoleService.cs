using System.Linq.Expressions;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public interface IRoleService
    {
        Task<ModelList<Role>> GetRoles(QueryOptions<Role>? options = null);
    }

    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<ModelList<Role>> GetRoles(QueryOptions<Role>? options = null)
        {
            Expression<Func<Role, bool>> filterExpression = null!;

            if (options != null)
            {
                filterExpression = options.FilterExpression;
            }

            if (!string.IsNullOrEmpty(options?.Filter))
            {
                filterExpression = f => f.Name.ToLower().Contains(options.Filter);
            }

            var roles = (await _roleRepository.FindAsync(filterExpression, 
                options?.OrderBy ?? string.Empty, options?.Page, null)).ToList();

            var rolesCount = await _roleRepository.CountAsync(filterExpression, null);

            var rolesList = ModelList.Create(roles, rolesCount);

            return rolesList;
        }
    }
}
