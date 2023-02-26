using Microsoft.AspNetCore.Mvc;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Management.Controllers
{
    [Route("Administration/Permissions")]
    public class UserPermissionsController : CommonController
    {
        private readonly IRoleRepository _roleRepository;

        public UserPermissionsController(IRoleRepository roleRepository, IUserRepository userRepository) : base(userRepository)
        {
            _roleRepository = roleRepository;
        }

        [HttpGet]
        public async Task<ActionResult<UserPermissions>> GetUserPermissions()
        {
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(CurrentUser.Id);

            return permissions;
        }

        [HttpGet("{userId:int}")]
        public async Task<ActionResult<UserPermissions>> GetUserPermissions(int userId)
        {
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);

            return permissions;
        }
    }
}
