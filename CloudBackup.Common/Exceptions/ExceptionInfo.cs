
namespace CloudBackup.Common.Exceptions
{
    public class ExceptionInfo
    {
        public string TypeName { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public ExceptionInfo? InnerException { get; set; }

        public ExceptionInfo() 
        {
            TypeName = string.Empty;
            Message = string.Empty;
            StackTrace = string.Empty;
        }

        public ExceptionInfo(Exception e)
        {
            TypeName = e.GetType().FullName ?? string.Empty;
            Message = e.Message ?? string.Empty;
            StackTrace = e.StackTrace ?? string.Empty;
            InnerException = e.InnerException != null ? new ExceptionInfo(e.InnerException) : null;
        }
    }

    public static class ExceptionInfoExtensions
    {
        public static ExceptionInfo ToExceptionInfo(this Exception exception)
        {
            return new ExceptionInfo(exception);
        }
    }
}