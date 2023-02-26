using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq.Expressions;
using CloudBackup.Common;
using CloudBackup.Common.Exceptions;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public class BackupFilter
    {
        public DateTime? MinFinishDate { get; set; }

        public DateTime? MaxFinishDate { get; set; }

        public BackupStatus? Status { get; set; }

        public bool OnlyPermanent { get; set; }

        public bool? RestoreEligible { get; set; }
    }

    public interface IBackupService
    {
        Task<ModelList<Backup>> GetBackupsAsync(int userId, QueryOptions<Backup>? options = null);

        Task<ModelList<Backup>> GetBackupsAsync(int userId, BackupFilter? backupFilter, QueryOptions<Backup>? options = null);

        Task CheckBackupIdsPermissions(ICollection<int> backupIds, int userId);
    }

    public class BackupService : IBackupService
    {
        public const string DefaultOrder = nameof(Backup.StartedAt) + "[desc]";

        private readonly IRepository<Backup> _backupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ITenantService _tenantService;

        public BackupService(
            IRepository<Backup> backupRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ITenantService tenantService)
        {
            _backupRepository = backupRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _tenantService = tenantService;
        }

        public async Task<ModelList<Backup>> GetBackupsAsync(int userId, QueryOptions<Backup>? options = null)
        {
            return await GetBackupsAsync(userId, null, options);
        }

        public async Task<ModelList<Backup>> GetBackupsAsync(int userId, BackupFilter? backupFilter, QueryOptions<Backup>? options = null)
        {
            var user = await _userRepository.FindByIdAsync(userId, null);

            if (user == null)
                throw new ObjectNotFoundException("User not found.");

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);
            if (!permissions.BackupRights.HasFlag(Permissions.Read))
                throw new UnauthorizedAccessException("No access to backups.");

            Expression<Func<Backup, bool>> filterExpression = null!;

            if (options != null)
            {
                filterExpression = options.FilterExpression;
            }

            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(userId);

            filterExpression = filterExpression.And(f => allowedTenantIds.Contains(f.Job.TenantId));

            var filter = options?.Filter;
            if (!string.IsNullOrEmpty(filter))
            {
                filterExpression = filterExpression.And(f => f.Name.ToLower().Contains(filter));
            }

            if (backupFilter != null)
            {
                if (backupFilter.MinFinishDate != null && backupFilter.MinFinishDate != DateTime.MinValue)
                {
                    filterExpression = filterExpression.And(
                        x => x.FinishedAt == null || x.FinishedAt >= backupFilter.MinFinishDate.Value.ToUniversalTime().Subtract(user.UtcOffset));
                }
                if (backupFilter.MaxFinishDate != null && backupFilter.MaxFinishDate != DateTime.MaxValue)
                {
                    filterExpression = filterExpression.And(
                        x => x.FinishedAt == null || x.FinishedAt <= backupFilter.MaxFinishDate.Value.ToUniversalTime().Subtract(user.UtcOffset));
                }

                if (backupFilter.Status != null)
                    filterExpression = filterExpression.And(x => x.Status == backupFilter.Status);

                if (backupFilter.OnlyPermanent)
                    filterExpression = filterExpression.And(x => x.IsPermanent);

                if (backupFilter.RestoreEligible != null)
                {
                    filterExpression = filterExpression.And(x =>
                        backupFilter.RestoreEligible == x.BackupObjects.Any(backupObject =>
                            backupObject.Status == BackupObjectStatus.Success || backupObject.Status == BackupObjectStatus.Warning));
                }
            }

            static IQueryable<Backup> Includes(IQueryable<Backup> query) => query.Include(x => x.Job).Include(x => x.BackupObjects);

            var orderBy = string.IsNullOrEmpty(options?.OrderBy) ? DefaultOrder : options?.OrderBy;

            var backups = (await _backupRepository.FindAsync(filterExpression, orderBy, options?.Page, Includes)).ToList();
            var count = await _backupRepository.CountAsync(filterExpression, Includes);

            return ModelList.Create(backups, count);
        }

        public async Task CheckBackupIdsPermissions(ICollection<int> backupIds, int userId)
        {
            if (backupIds == null || !backupIds.Any())
            {
                return;
            }

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);
            if (!permissions.BackupRights.HasFlag(Permissions.Read))
                throw new UnauthorizedAccessException("No access for backups.");

            var backups = await _backupRepository.FindByIdsAsync(backupIds, q => q.Include(x => x.Job));
            var allowedTenantIds = (await _tenantService.GetAllowedTenantIds(userId)).ToImmutableHashSet();

            if (backups.Any(backup => !allowedTenantIds.Contains(backup.Job.TenantId)))
                throw new UnauthorizedAccessException("No permissions to view backups of this tenant.");
        }
    }
}
