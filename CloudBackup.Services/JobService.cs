using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq.Expressions;
using CloudBackup.Common;
using CloudBackup.Common.Exceptions;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public interface IJobService
    {
        Task<ModelList<Job>> GetJobsAsync(int userId, QueryOptions<Job>? options = null);

        Task<Job> GetJobAsync(int userId, int jobId);
    }

    public class JobService : IJobService
    {
        private const string DefaultJobOrder = nameof(Job.LastRunAt) + "[desc]";

        private readonly IRepository<Job> _jobRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ITenantService _tenantService;

        public JobService(
            IRepository<Job> jobRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ITenantService tenantService)
        {
            _jobRepository = jobRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _tenantService = tenantService;
        }

        public async Task<ModelList<Job>> GetJobsAsync(int userId, QueryOptions<Job>? options = null)
        {
            // Check permissions
            var currentUser = await _userRepository.FindByIdAsync(userId, null);

            if (currentUser == null)
                throw new ObjectNotFoundException("User not found.");

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);

            if (!permissions.BackupRights.HasFlag(Permissions.Read))
                throw new UnauthorizedAccessException("No permissions to view backup jobs.");

            // Filter
            Expression<Func<Job, bool>> filterExpression = null!;

            if (options != null)
            {
                filterExpression = options.FilterExpression;
            }

            var tenantIds = await _tenantService.GetAllowedTenantIds(currentUser.Id);
            filterExpression = filterExpression.And(f => tenantIds.Contains(f.TenantId));

            if (!string.IsNullOrEmpty(options?.Filter))
            {
                var filterLower = options.Filter.ToLower();
                filterExpression = filterExpression.And(f => f.Name.ToLower().Contains(filterLower) ||
                                                             f.Tenant.Name.Contains(filterLower));
            }

            // Get data
            static IQueryable<Job> Includes(IQueryable<Job> query) => query
                .Include(p => p.Schedule)
                .Include(p => p.Configuration)
                .Include(p => p.User)
                .Include(p => p.JobObjects)
                .Include(p => p.User)
                .Include(p => p.Tenant);

            var orderBy = string.IsNullOrEmpty(options?.OrderBy) ? DefaultJobOrder : options?.OrderBy;

            var jobs = await _jobRepository.FindAsync(filterExpression, orderBy, options?.Page, Includes);

            var totalCount = await _jobRepository.CountAsync(filterExpression, Includes);

            return ModelList.Create(jobs, totalCount);
        }

        public async Task<Job> GetJobAsync(int userId, int jobId)
        {
            await CheckUserPermissions(userId, Permissions.Read);

            static IQueryable<Job> Includes(IQueryable<Job> query) => query
                .Include(p => p.Schedule)
                .Include(p => p.User)
                .Include(p => p.JobObjects)
                .Include(p => p.User)
                .Include(p => p.Tenant);

            var job = await _jobRepository.FindByIdAsync(jobId, Includes);

            if (job == null)
                throw new ObjectNotFoundException("Job not found.");

            var tenantIds = await _tenantService.GetAllowedTenantIds(userId);
            if (!tenantIds.Contains(job.TenantId))
                throw new UnauthorizedAccessException("No permissions to access job from another tenant.");

            return job;
        }

        private async Task<User> CheckUserPermissions(int userId, Permissions requiredPermissions)
        {
            var user = await _userRepository.FindByIdAsync(userId, null);
            if (user == null)
                throw new ObjectNotFoundException("User not found.");

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);

            if (!permissions.BackupRights.HasFlag(requiredPermissions))
                throw new UnauthorizedAccessException($"No '{requiredPermissions}' permission for backup jobs.");

            return user;
        }
    }
}