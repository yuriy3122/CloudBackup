using System.Net;
using System.Text;
using System.Security.Authentication;
using Newtonsoft.Json;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common.Exceptions;

namespace CloudBackup.Management.ErrorHandling
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IRepository<Log> logRepository)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, logRepository);
            }
        }

        private async static Task HandleExceptionAsync(HttpContext context, Exception exception, IRepository<Log> logRepository)
        {
            var code = HttpStatusCode.InternalServerError;
            string errorMessage = string.Empty;

            switch (exception)
            {
                case AuthenticationException _:
                case UnauthorizedAccessException _:
                case DisabledUserException _:
                    code = HttpStatusCode.Unauthorized;
                    errorMessage = exception.Message;
                    break;
                case NotSupportedException _:
                case InvalidObjectException _:
                case RestoreFailedException _:
                case DeleteBackupJobException _:
                    code = HttpStatusCode.BadRequest;
                    errorMessage = exception.Message;
                    break;
                case ArgumentException _:
                    code = HttpStatusCode.BadRequest;
                    break;
                case ObjectNotFoundException _:
                    code = HttpStatusCode.NotFound;
                    errorMessage = exception.Message;
                    break;
                case ReferencedItemException _:
                    code = HttpStatusCode.Forbidden;
                    errorMessage = exception.Message;
                    break;
                case BadRequestException _:
                    code = HttpStatusCode.BadRequest;
                    errorMessage = exception.Message;
                    break;
            }

            var logDataBuilder = new StringBuilder();

            if (context.Request != null)
            {
                logDataBuilder.AppendLine("Request:");
                logDataBuilder.AppendLine($"{context.Request.Method} {context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
                logDataBuilder.AppendLine();
            }

            logDataBuilder.AppendLine("Exception:");
            logDataBuilder.AppendLine(exception.ToString());

            var logEntry = exception.ToLogEntry();
            logEntry.XmlData = logDataBuilder.ToString();

            logRepository.Add(logEntry);
            await logRepository.SaveChangesAsync();

            var result = JsonConvert.SerializeObject(new { error = exception.Message, message = errorMessage });
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            await context.Response.WriteAsync(result);
        }
    }
}