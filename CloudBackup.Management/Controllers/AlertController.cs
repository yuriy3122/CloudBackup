using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Common.Exceptions;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Alerts")]
    public class AlertController : CommonController
    {
        private const string DefaultOrder = nameof(Alert.Date) + "[desc]";

        private readonly IRepository<Alert> _alertRepository;
        private readonly IRepository<UserAlert> _userAlertRepository;

        public AlertController(
            IRepository<Alert> alertRepository,
            IRepository<UserAlert> userAlertRepository,
            IUserRepository userRepository) : base(userRepository)
        {
            _alertRepository = alertRepository;
            _userAlertRepository = userAlertRepository;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<AlertViewModel>>> GetAlerts(DateTime? minDate, bool? onlyNew, int? pageSize, int? pageNum, string order, string filter)
        {
            Expression<Func<Alert, bool>> filterExpression = alert => 
                alert.UserAlerts != null && alert.UserAlerts.Any(x => x.UserId == CurrentUser.Id && (x.IsNew || onlyNew != true));
            
            if (minDate != null)
            {
                filterExpression = filterExpression.And(alert => alert.Date >= minDate.Value.ToUniversalTime());
            }

            if (!string.IsNullOrEmpty(filter))
            {
                var filterLower = filter.ToLower();
                filterExpression = filterExpression.And(f => (f.Message != null && f.Message.ToLower().Contains(filterLower)) || 
                                                             (f.Subject != null && f.Subject.ToLower().Contains(filterLower)));
            }

            var page = pageSize.HasValue && pageNum.HasValue ? new EntitiesPage(pageSize.Value, pageNum.Value) : null;

            var alerts = await _alertRepository.FindAsync(filterExpression, order ?? DefaultOrder, page, query => query.Include(x => x.UserAlerts));
            var alertsCount = await _alertRepository.CountAsync(filterExpression, query => query.Include(x => x.UserAlerts));

            var alertViewModels = alerts.Select(
                    alert => new AlertViewModel(alert)
                    {
                        IsNew = alert.UserAlerts != null && alert.UserAlerts.Single(x => x.UserId == CurrentUser.Id).IsNew
                    })
                .ToList();

            foreach (var alertViewModel in alertViewModels)
            {
                if (alertViewModel.Date != DateTime.MinValue)
                    alertViewModel.Date += CurrentUser.UtcOffset;
            }
            
            var alertsViewModelList = ModelList.Create(alertViewModels, alertsCount);

            return alertsViewModelList;
        }

        [HttpGet]
        [Route("Count")]
        public async Task<ActionResult<int>> GetAlertsCount(bool? onlyNew)
        {
            Expression<Func<Alert, bool>> filterExpression = alert => 
                alert.UserAlerts != null && alert.UserAlerts.Any(x => x.UserId == CurrentUser.Id && (x.IsNew || onlyNew != true));

            var alertsCount = await _alertRepository.CountAsync(filterExpression, query => query.Include(x => x.UserAlerts));

            return alertsCount;
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> RemoveAlert(int id)
        {
            var userAlert = (await _userAlertRepository.FindAsync(x => x.UserId == CurrentUser.Id && x.AlertId == id, null, null, null)).SingleOrDefault();

            if (userAlert == null)
            {
                throw new ObjectNotFoundException("Alert not found.");
            }

            _userAlertRepository.Remove(userAlert);

            await _userAlertRepository.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Route("Delete")]
        public async Task<IActionResult> RemoveAlerts([FromBody] int[] ids)
        {
            Expression<Func<UserAlert, bool>> filterExpression =
                userAlert => userAlert.UserId == CurrentUser.Id && ids.Contains(userAlert.AlertId);

            var userAlerts = await _userAlertRepository.FindAsync(filterExpression, null, null, null);

            foreach (var userAlert in userAlerts)
            {
                _userAlertRepository.Remove(userAlert);
            }

            await _userAlertRepository.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Route("Read")]
        public async Task<IActionResult> MarkRead([FromBody] int[] ids)
        {
            Expression<Func<UserAlert, bool>> filterExpression =
                userAlert => userAlert.UserId == CurrentUser.Id && ids.Contains(userAlert.AlertId);

            var userAlerts = await _userAlertRepository.FindAsync(filterExpression, null, null, null);

            foreach (var userAlert in userAlerts)
            {
                userAlert.IsNew = false;
                _userAlertRepository.Update(userAlert);
            }

            await _userAlertRepository.SaveChangesAsync();

            return Ok();
        }
    }

    public class AlertViewModel
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string DateText => DateTimeHelper.Format(Date);

        public AlertType Type { get; set; }

        public string Message { get; set; }

        public string Subject { get; set; }

        public bool IsNew { get; set; }

        public AlertViewModel(Alert alert)
        {
            Id = alert.Id;
            Date = alert.Date;
            Type = alert.Type;
            Message = alert.Message ?? string.Empty;
            Subject = alert.Subject ?? string.Empty;
        }
    }
}
