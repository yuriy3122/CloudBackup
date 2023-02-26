using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Common;
using CloudBackup.Common.Exceptions;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public interface IScheduleService
    {
        Task<ModelList<Schedule>> GetSchedulesAsync(int userId, int? tenantId, StartupType? startupType = null, QueryOptions<Schedule>? options = null);

        Task<Schedule> GetScheduleAsync(int userId, int scheduleId);

        Task<Schedule> AddScheduleAsync(int userId, Schedule schedule);

        Task UpdateScheduleAsync(int userId, int scheduleId, Schedule updatedSchedule);

        Task DeleteScheduleAsync(int userId, int scheduleId);

        string ConvertScheduleDatesToUtc(Schedule schedule, TimeSpan utcOffset);
    }

    public class ScheduleService : IScheduleService
    {
        private const string DefaultScheduleOrder = nameof(Schedule.Name);

        private readonly IRepository<Schedule> _scheduleRepository;
        private readonly IRepository<Job> _jobRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ITenantService _tenantService;

        public ScheduleService(
            IRepository<Schedule> scheduleRepository,
            IRepository<Job> jobRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ITenantService tenantService)
        {
            _scheduleRepository = scheduleRepository;
            _jobRepository = jobRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _tenantService = tenantService;
        }

        public async Task<ModelList<Schedule>> GetSchedulesAsync(int userId, int? tenantId, StartupType? startupType = null, QueryOptions<Schedule>? options = null)
        {
            await CheckUserPermissions(userId, Permissions.Read);

            Expression<Func<Schedule, bool>> filterExpression = null!;
            
            if (options != null)
            {
                filterExpression = options.FilterExpression;
            }

            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(userId);

            if (tenantId.HasValue)
            {
                if (!allowedTenantIds.Contains(tenantId.Value))
                {
                    throw new UnauthorizedAccessException("No access to this tenant.");
                }

                allowedTenantIds = new[] { tenantId.Value };
            }

            filterExpression = filterExpression.And(x => allowedTenantIds.Contains(x.TenantId));

            if (!string.IsNullOrEmpty(options?.Filter))
            {
                var filterLower = options.Filter.ToLower();
                var allowedOccurTypes = EnumHelper.GetValues<OccurType>()
                    .Where(x => x.ToString().ToLower().Contains(filterLower))
                    .Cast<OccurType?>()
                    .ToList();

                filterExpression = filterExpression.And(f => f.Name.ToLower().Contains(filterLower) ||
                                                             f.Tenant.Name.ToLower().Contains(filterLower) ||
                                                             allowedOccurTypes.Contains(f.OccurType));
            }

            if (startupType != null)
            {
                filterExpression = filterExpression.And(x => x.StartupType == startupType);
            }

            var orderBy = string.IsNullOrEmpty(options?.OrderBy) ? DefaultScheduleOrder : options?.OrderBy;

            static IQueryable<Schedule> Includes(IQueryable<Schedule> query) => query.Include(x => x.Tenant);
            var schedules = await _scheduleRepository.FindAsync(filterExpression, orderBy, options?.Page, Includes);

            var totalCount = await _scheduleRepository.CountAsync(filterExpression, Includes);

            return ModelList.Create(schedules, totalCount);
        }

        public async Task<Schedule> GetScheduleAsync(int userId, int scheduleId)
        {
            var currentUser = await _userRepository.FindByIdAsync(userId, null);

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);

            if (!permissions.BackupRights.HasFlag(Permissions.Read))
            {
                throw new UnauthorizedAccessException("No access to backup jobs.");
            }

            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(currentUser.Id);

            var schedules = await _scheduleRepository.FindAsync(x => x.Id == scheduleId && allowedTenantIds.Contains(x.TenantId), 
                string.Empty, null, null);

            var schedule = schedules.FirstOrDefault();

            if (schedule == null)
            {
                throw new ObjectNotFoundException($"Schedule with id={scheduleId} does not exist.");
            }

            return schedule;
        }

        public async Task<Schedule> AddScheduleAsync(int userId, Schedule schedule)
        {
            var currentUser = await CheckUserPermissions(userId, Permissions.Write);

            if (schedule.Id != 0)
            {
                throw new InvalidObjectException("Can't add existing schedule.");
            }

            if (schedule.TenantId > 0)
            {
                var allowedTenantIds = await _tenantService.GetAllowedTenantIds(currentUser.Id);

                if (!allowedTenantIds.Contains(schedule.TenantId))
                {
                    throw new UnauthorizedAccessException("No permissions to add schedule to another tenant.");
                }
            }

            var scheduleToAdd = new Schedule
            {
                Name = schedule.Name,
                JobCount = 0,
                OccurType = schedule.OccurType,
                StartupType = schedule.StartupType,
                TenantId = schedule.TenantId != 0 ? schedule.TenantId : currentUser.TenantId,
                Params = ConvertScheduleDatesToUtc(schedule, currentUser.UtcOffset),
                Created = DateTime.UtcNow
            };

            _scheduleRepository.Add(scheduleToAdd);

            await _scheduleRepository.SaveChangesAsync();

            return scheduleToAdd;
        }

        public async Task UpdateScheduleAsync(int userId, int scheduleId, Schedule updatedSchedule)
        {
            var currentUser = await CheckUserPermissions(userId, Permissions.Write);

            var schedule = await _scheduleRepository.FindByIdAsync(scheduleId, null);

            if (schedule == null)
            {
                throw new ObjectNotFoundException("Schedule not found.");
            }

            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(currentUser.Id);

            if (!allowedTenantIds.Contains(schedule.TenantId))
            {
                throw new UnauthorizedAccessException("No permissions to modify schedules from another tenant.");
            }

            if (updatedSchedule.StartupType != StartupType.Recurring && schedule.StartupType == StartupType.Recurring)
            {
                throw new NotSupportedException($"Can't change schedule type from {schedule.StartupType} to {updatedSchedule.StartupType}.");
            }

            if (updatedSchedule.TenantId != schedule.TenantId && schedule.JobCount > 0)
            {
                throw new NotSupportedException("Can't change tenant of a schedule which is used in jobs.");
            }

            schedule.Name = updatedSchedule.Name;
            schedule.TenantId = updatedSchedule.TenantId;
            schedule.StartupType = updatedSchedule.StartupType;
            schedule.OccurType = updatedSchedule.OccurType;

            var oldScheduleParams = schedule.Params;
            var newScheduleParams = ConvertScheduleDatesToUtc(updatedSchedule, currentUser.UtcOffset);
            schedule.Params = newScheduleParams;

            _scheduleRepository.Update(schedule);
            await _scheduleRepository.SaveChangesAsync();

            // Reset affected job's next run dates
            if (newScheduleParams != oldScheduleParams)
            {
                var jobs = await _jobRepository.FindAsync(x => x.ScheduleId == scheduleId, string.Empty, null, null);

                foreach (var job in jobs)
                {
                    job.NextRunAt = null;
                    _jobRepository.Update(job);
                }

                await _jobRepository.SaveChangesAsync();
            }
        }

        public async Task DeleteScheduleAsync(int userId, int scheduleId)
        {
            var currentUser = await CheckUserPermissions(userId, Permissions.Write);

            var jobsCount = await _jobRepository.CountAsync(x => x.ScheduleId == scheduleId, null);

            if (jobsCount > 0)
            {
                throw new NotSupportedException("Can't delete a schedule which is used in any jobs.");
            }

            var schedule = await _scheduleRepository.FindByIdAsync(scheduleId, null);

            if (schedule == null)
            {
                throw new ObjectNotFoundException("Schedule not found.");
            }

            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(currentUser.Id);

            if (!allowedTenantIds.Contains(schedule.TenantId))
            {
                throw new UnauthorizedAccessException("No permissions to modify schedules from another tenant.");
            }

            _scheduleRepository.Remove(schedule);

            await _scheduleRepository.SaveChangesAsync();
        }

        public string ConvertScheduleDatesToUtc(Schedule schedule, TimeSpan utcOffset)
        {
            var scheduleParam = ScheduleParamFactory.CreateScheduleParam(schedule);

            if (scheduleParam != null)
            {
                return scheduleParam.ConvertDatesToUtc(utcOffset);
            }

            return string.Empty;
        }

        private async Task<User> CheckUserPermissions(int userId, Permissions requiredPermissions)
        {
            var user = await _userRepository.FindByIdAsync(userId, null);

            if (user == null)
            {
                throw new ObjectNotFoundException("User not found.");
            }

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);

            if (!permissions.BackupRights.HasFlag(requiredPermissions))
            {
                throw new UnauthorizedAccessException($"No '{requiredPermissions}' permission for schedules.");
            }

            return user;
        }
    }
}