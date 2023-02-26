using Microsoft.AspNetCore.Mvc;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Services;

namespace CloudBackup.Management.Controllers
{
    [Route("Administration/Role")]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<RoleViewModel>>> GetRoles(int? pageSize, int? pageNum, string order, string filter)
        {
            var rolesList = await _roleService.GetRoles(new QueryOptions<Role>(
                new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1), order, filter));

            var roleViewModels = rolesList.Items.Select(role => new RoleViewModel(role)).ToList();
            var roleViewModelList = ModelList.Create(roleViewModels, rolesList.TotalCount);

            return roleViewModelList;
        }
    }

    public class RoleViewModel
    {
        public int Id { get; set; }
        public string? RowVersion { get; set; }
        public string? Name { get; set; }
        public bool IsGlobalAdmin { get; set; }

        public RoleViewModel()
        {
        }

        public RoleViewModel(Role role)
        {
            Id = role.Id;
            RowVersion = role.RowVersion != null ? Convert.ToBase64String(role.RowVersion) : null;
            Name = role.Name;
            IsGlobalAdmin = role.IsGlobalAdmin;
        }

        public Role GetRole()
        {
            return new Role
            {
                Id = Id,
                RowVersion = Convert.FromBase64String(RowVersion ?? string.Empty),
                Name = Name ?? string.Empty,
                IsGlobalAdmin = IsGlobalAdmin
            };
        }
    }
}