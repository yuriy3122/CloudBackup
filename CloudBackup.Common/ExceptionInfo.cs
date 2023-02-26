
namespace CloudBackup.Common
{
    public class ExceptionInfo
    {
        public string? TypeName { get; set; }

        public string? Message { get; set; }

        public string? StackTrace { get; set; }

        public ExceptionInfo? InnerException { get; set; }

        public ExceptionInfo() { }

        public ExceptionInfo(Exception e)
        {
            TypeName = e.GetType().FullName;
            Message = e.Message;
            StackTrace = e.StackTrace;
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
