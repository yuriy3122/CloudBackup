using CloudBackup.Model;
using CloudBackup.Common.Exceptions;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public interface ITimeZoneService
    {
        IReadOnlyCollection<TimeZoneInfo> GetTimeZones();

        Task<TimeSpan> GetUserUtcOffsetAsync(int userId);
    }

    public class TimeZoneService : ITimeZoneService
    {
        private readonly IUserRepository _userRepository;

        public TimeZoneService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public IReadOnlyCollection<TimeZoneInfo> GetTimeZones()
        {
            var zones = TimeZoneInfo.GetSystemTimeZones();

            return zones;
        }

        public async Task<TimeSpan> GetUserUtcOffsetAsync(int userId)
        {
            var user = await _userRepository.FindByIdAsync(userId, null);

            if (user == null)
            {
                throw new ObjectNotFoundException("User not found.");
            }

            return user.UtcOffset;
        }
    }
}
