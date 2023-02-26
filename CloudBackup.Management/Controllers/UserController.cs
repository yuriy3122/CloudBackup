using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Services;
using CloudBackup.Common.Exceptions;

namespace CloudBackup.Management.Controllers
{
    [Route("Administration/User")]
    public class UserController : CommonController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService, IUserRepository userRepository) : base(userRepository)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<UserViewModel>>> GetUsers(int? pageSize, int? pageNum, string order, string filter)
        {
            if (order != null)
            {
                order = Regex.Replace(order,
                    $"{nameof(UserViewModel.UtcOffset)}.{nameof(TimeZoneViewModel.Name)}",
                    nameof(Model.User.UtcOffsetTicks),
                    RegexOptions.IgnoreCase);
            }

            var modelList = await _userService.GetUsersAsync(CurrentUser.Id, 
                new QueryOptions<User>(new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1), order ?? string.Empty, filter));

            var viewModelList = GetViewModelList(modelList, CurrentUser);

            return viewModelList;
        }

        [HttpGet]
        [Route("{userId:int}")]
        public async Task<ActionResult<UserViewModel>> GetUser(int userId)
        {
            var options = new QueryOptions<User> { FilterExpression = x => x.Id == userId };

            var modelList = await _userService.GetUsersAsync(CurrentUser.Id, options);
            var viewModelList = GetViewModelList(modelList, CurrentUser);

            var userViewModel = viewModelList.Items.SingleOrDefault();

            if (userViewModel == null)
            {
                throw new ObjectNotFoundException("There is no user with this id.");
            }

            return userViewModel;
        }

        [HttpGet]
        [Route("Current")]
        public ActionResult<UserViewModel> GetCurrentUser()
        {
            var userViewModel = new UserViewModel(CurrentUser);

            return userViewModel;
        }

        [HttpGet]
        [Route("Login/{login}")]
        public async Task<ActionResult<UserViewModel>> GetUserByLogin(string login)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentNullException(nameof(login));

            var user = await _userService.GetUserByLoginAsync(login);

            var userViewModel = new UserViewModel(user);

            return userViewModel;
        }

        [HttpGet]
        [Route("CheckLogin/{login}")]
        public async Task<ActionResult<bool>> CheckLogin(string login)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentNullException(nameof(login));

            bool userExists;

            try
            {
                var user = await _userService.GetUserByLoginAsync(login);
                userExists = user != null;
            }
            catch (ObjectNotFoundException)
            {
                userExists = false;
            }

            return userExists;
        }

        [HttpPost]
        public async Task<ActionResult<UserViewModel>> AddUser([FromBody] UserViewModel userViewModel)
        {
            if (userViewModel == null)
                throw new ArgumentNullException(nameof(userViewModel));

            var user = userViewModel.GetUser();

            var newUser = await _userService.AddUserAsync(CurrentUser.Id, user, userViewModel.Password ?? string.Empty);
            var newUserViewModel = new UserViewModel(newUser);

            return CreatedAtAction(nameof(GetUser), new {userId = newUser.Id}, newUserViewModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserViewModel userViewModel)
        {
            var user = userViewModel.GetUser();

            await _userService.UpdateUserAsync(CurrentUser.Id, id, user, userViewModel.Password ?? string.Empty);

            return NoContent();
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> RemoveUser(int id)
        {
            await _userService.RemoveUserAsync(CurrentUser.Id, id);

            return NoContent();
        }

        private ModelList<UserViewModel> GetViewModelList(ModelList<User> modelList, User currentUser)
        {
            var userViewModels = modelList.Items.Select((user, i) =>
            {
                var userViewModel = new UserViewModel(user);

                if (userViewModel.Id == currentUser.Id)
                {
                    userViewModel.CanDelete = false;
                    userViewModel.CantDeleteReason = "Cannot delete yourself.";
                }

                return userViewModel;
            }).ToList();

            var viewModelList = ModelList.Create(userViewModels, modelList.TotalCount);
            return viewModelList;
        }
    }

    public class UserViewModel
    {
        public int Id { get; set; } // hidden field
        public string? RowVersion { get; set; }//hidden field
        public int? RowIndex { get; set; }

        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; } // input-only
        public string? Email { get; set; }
        public bool IsEnabled { get; set; }
        public UtcOffsetViewModel? UtcOffset { get; set; }
        public TenantViewModel? Tenant { get; set; }
        public UserViewModel? Owner { get; set; }
        public List<RoleViewModel>? Roles { get; set; }
        public RoleViewModel? Role
        {
            get => Roles?.FirstOrDefault() ?? new RoleViewModel();
            set => Roles = value != null ? new List<RoleViewModel> { value } : new List<RoleViewModel>();
        }

        public bool? CanDelete { get; set; } = true;
        public string? CantDeleteReason { get; set; }

        public UserViewModel()
        {
        }

        public UserViewModel(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            Id = user.Id;
            RowVersion = user.RowVersion == null ? null : Convert.ToBase64String(user.RowVersion);
            Name = user.Name;
            Description = user.Description;
            Login = user.Login;
            Email = user.Email;
            IsEnabled = user.IsEnabled;
            UtcOffset = new UtcOffsetViewModel(user.UtcOffset);
            Tenant = user.Tenant != null
                ? new TenantViewModel(user.Tenant)
                : (user.TenantId != 0 ? new TenantViewModel {Id = user.TenantId} : null);
            Owner = user.Owner != null
                ? new UserViewModel
                {
                    Id = user.Owner.Id,
                    Name = user.Owner.Name,
                    Description = user.Owner.Description,
                    Roles = user.Owner.UserRoles?.Where(x => x.Role != null).Select(userRole => new RoleViewModel(userRole.Role)).ToList()
                }
                : (user.OwnerUserId != null ? new UserViewModel { Id = user.OwnerUserId.Value } : null);
            Roles = user.UserRoles?
                .Select(userRole => userRole.Role != null
                    ? new RoleViewModel(userRole.Role)
                    : new RoleViewModel {Id = userRole.RoleId})
                .ToList();
        }

        public User GetUser()
        {
            var user = new User
            {
                Id = Id,
                RowVersion = Convert.FromBase64String(RowVersion ?? string.Empty),
                Name = Name ?? string.Empty,
                Description = Description ?? string.Empty,
                Login = Login ?? string.Empty,
                Email = Email ?? string.Empty,
                IsEnabled = IsEnabled,
                UtcOffset = UtcOffset?.Offset ?? TimeSpan.Zero,
                TenantId = Tenant?.Id ?? 0,
                Tenant = Tenant?.GetTenant() ?? new Tenant(),
                OwnerUserId = Owner?.Id,
                Owner = Owner?.GetUser() ?? new User()
            };

            user.UserRoles = Roles!.Select(role => new UserRole
            {
                RoleId = role.Id,
                Role = role.GetRole(),
                UserId = user.Id,
                User = user
            }).ToList();

            return user;
        }
    }
}