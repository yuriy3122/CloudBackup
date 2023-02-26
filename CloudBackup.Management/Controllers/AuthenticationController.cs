using System.Text;
using System.Security.Claims;
using System.Security.Principal;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using Managment.Authorization;
using CloudBackup.Common.Exceptions;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Authentication")]
    public class AuthenticationController : Controller
    {
        private readonly JwtIssuerOptions _jwtOptions;
        private readonly IUserRepository _userRepository;

        public AuthenticationController(
            IOptions<JwtIssuerOptions> jwtOptions,
            IUserRepository userRepository,
            IConfiguration configuration)
        {
            _jwtOptions = jwtOptions.Value;
            _jwtOptions.Audience = configuration.GetSection("TokenAuthentication:Audience").Value;
            _jwtOptions.Issuer = configuration.GetSection("TokenAuthentication:Issuer").Value;

            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration.GetSection("TokenAuthentication:SecretKey").Value));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            _jwtOptions.SigningCredentials = credentials;

            _userRepository = userRepository;

            ThrowIfInvalidOptions(jwtOptions.Value);
        }

        [AllowAnonymous]
        [HttpPost("Bearer")]
        public async Task<ActionResult<JwtResponse>> BearerAuthentication(string credentialsEncoded)
        {
            var credentials = JObject.Parse(credentialsEncoded.Base64Decode());
            var username = credentials?["username"]?.ToString();
            var password = credentials?["password"]?.ToString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return Unauthorized();
            }

            var identity = await GetClaimsIdentity(username, password);

            if (identity == null)
            {
                return Unauthorized();
            }

            var response = GetEncodedJwtResponse(identity);

            return Ok(response);
        }

        private async Task<ClaimsIdentity> GetClaimsIdentity(string username, string? password = null)
        {
            var users = await _userRepository.FindAsync(f => f.Login == username, string.Empty, null, null);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                throw new AuthenticationException("Invalid login");
            }

            if (password == null)
            {
                throw new AuthenticationException("Invalid password");
            }

            if (!ValidateCustomUserPassword(password, user))
            {
                throw new AuthenticationException("Invalid password");
            }

            if (!user.IsEnabled)
            {
                throw new DisabledUserException("User disabled");
            }

            var identity = await Task.FromResult(new ClaimsIdentity(new GenericIdentity(username), new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.Name, username)
            }));

            if (identity == null)
            {
                throw new AuthenticationException();
            }

            return identity;
        }

        private static bool ValidateCustomUserPassword(string password, User user)
        {
            return PasswordHelper.GetPasswordHash(password ?? string.Empty, user.PasswordSalt ?? string.Empty) == user.PasswordHash;
        }

        private JwtResponse GetEncodedJwtResponse(ClaimsIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            var jwt = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: identity.Claims,
                notBefore: _jwtOptions.NotBefore,
                expires: _jwtOptions.Expiration,
                signingCredentials: _jwtOptions.SigningCredentials);

            var jwtHandler = new JwtSecurityTokenHandler();
            var encodedJwt = jwtHandler.WriteToken(jwt);

            var response = new JwtResponse
            {
                access_token = encodedJwt,
                expires_in = (int)_jwtOptions.ValidFor.TotalSeconds
            };

            return response;
        }

        private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.ValidFor <= TimeSpan.Zero)
            {
                throw new ArgumentException(nameof(JwtIssuerOptions.ValidFor));
            }

            if (options.SigningCredentials == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
            }

            if (options.JtiGenerator == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
            }
        }

        /// <returns>Date converted to seconds since Unix epoch (Jan 1, 1970, midnight UTC).</returns>
        private static long ToUnixEpochDate(DateTime date) =>
            (long)Math.Floor((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);

        public class JwtResponse
        {
#pragma warning disable IDE1006 // Naming Styles
            public string access_token { get; set; } = string.Empty;

            public int expires_in { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        }
    }
}