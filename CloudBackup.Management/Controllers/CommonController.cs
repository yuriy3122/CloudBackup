using System.Security.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using CloudBackup.Model;
using CloudBackup.Common.Exceptions;
using CloudBackup.Repositories;

namespace CloudBackup.Management.Controllers
{
    public class CommonController : Controller
    {
        private readonly IUserRepository _userRepository;

        public CommonController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public User CurrentUser { get; set; } = null!;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.Filters.OfType<AllowAnonymousFilter>().Any())
            {
                var userName = context.HttpContext.User?.Identity?.Name;

                if (userName != null)
                {
                    var users = await _userRepository.FindAsync(f => f.Login == userName, string.Empty, null, null);
                    var user = users?.FirstOrDefault();

                    if (user == null)
                    {
                        throw new AuthenticationException("Invalid login.");
                    }

                    if (!user.IsEnabled)
                    {
                        throw new DisabledUserException();
                    }

                    CurrentUser = user;
                }
                else if (!context.Filters.OfType<AllowAnonymousFilter>().Any())
                {
                    context.Result = Unauthorized();
                }
            }

            await next.Invoke();
        }
    }
}