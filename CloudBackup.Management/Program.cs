using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using CloudBackup.Common;
using CloudBackup.Model;
using CloudBackup.Services;
using CloudBackup.Repositories;
using CloudBackup.Management.WebSockets;
using CloudBackup.Management.ErrorHandling;
using CloudBackup.Management.MessageHandling;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddMemoryCache();
builder.Services.AddDbContextPool<BackupContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddTransient<IRepository<Log>, LogRepository>();
builder.Services.AddTransient<IRepository<Job>, JobRepository>();
builder.Services.AddTransient<IRepository<JobConfiguration>, JobConfigurationRepository>();
builder.Services.AddTransient<IRepository<JobObject>, JobObjectRepository>();
builder.Services.AddTransient<IRepository<Backup>, BackupRepository>();
builder.Services.AddTransient<IRepository<BackupLog>, BackupLogRepository>();
builder.Services.AddTransient<IRepository<BackupObject>, BackupObjectRepository>();
builder.Services.AddTransient<IRepository<Profile>, ProfileRepository>();
builder.Services.AddTransient<IRepository<RestoreJob>, RestoreJobRepository>();
builder.Services.AddTransient<IRepository<RestoreJobObject>, RestoreJobObjectRepository>();
builder.Services.AddTransient<IRepository<Role>, RoleRepository>();
builder.Services.AddTransient<IRepository<Schedule>, ScheduleRepository>();
builder.Services.AddTransient<IRepository<Tenant>, TenantRepository>();
builder.Services.AddTransient<IRepository<TenantProfile>, TenantProfileRepository>();
builder.Services.AddTransient<IRepository<UserRole>, UserRoleRepository>();
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IRoleRepository, RoleRepository>();
builder.Services.AddTransient<IRepository<Alert>, AlertRepository>();
builder.Services.AddTransient<IRepository<UserAlert>, UserAlertRepository>();
builder.Services.AddTransient<IRepository<NotificationConfiguration>, NotificationConfigurationRepository>();
builder.Services.AddTransient<IRepository<NotificationDeliveryConfiguration>, NotificationDeliveryConfigurationRepository>();
builder.Services.AddTransient<IRepository<Configuration>, ConfigurationRepository>();
builder.Services.AddTransient<ITimeZoneService, TimeZoneService>();
builder.Services.AddTransient<ITenantService, TenantService>();
builder.Services.AddTransient<IScheduleService, ScheduleService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IProfileService, ProfileService>();
builder.Services.AddTransient<ICostService, CostService>();
builder.Services.AddTransient<IRoleService, RoleService>();
builder.Services.AddTransient<IJobService, JobService>();
builder.Services.AddTransient<IBackupService, BackupService>();
builder.Services.AddTransient<ICloudClientFactory, CloudClientFactory>();
builder.Services.AddTransient<IComputeCloudClientFactory, ComputeCloudClientFactory>();
builder.Services.AddSingleton<ILogger>(svc => svc.GetRequiredService<ILogger<Program>>());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration.GetSection("TokenAuthentication:SecretKey").Value));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidAudience = builder.Configuration.GetSection("TokenAuthentication:Audience").Value,
            ValidIssuer = builder.Configuration.GetSection("TokenAuthentication:Issuer").Value
        };
    });

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddWebSocketManager();
builder.Services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });
builder.Services.Configure<IISServerOptions>(options => { options.AllowSynchronousIO = true; });

var app = builder.Build();

// Configure middleware
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseWebSockets();
app.MapWebSocketManager("/notifications", app.Services.GetService<JobMessageHandler>());
app.MapWebSocketManager("/alerts", app.Services.GetService<AlertMessageHandler>());
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");
app.MapControllers().RequireAuthorization();

app.Run();