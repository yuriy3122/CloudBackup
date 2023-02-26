using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using CloudBackup.Management.Controllers;
using CloudBackup.Model;
using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace CloudBackup.UnitTests
{
    internal static class UnitTestsHelper
    {
        public static void SetUserIdentity(ControllerBase controller, User user)
        {
            controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new List<ClaimsIdentity>
                    {
                        new ClaimsIdentity(new[]
                            {
                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                new Claim(ClaimTypes.Name, user.Login)
                            }
                        )
                    })
            };

            if (controller is CommonController commonController)
            {
                commonController.CurrentUser = user;
            }
        }
    }
}
