
namespace CloudBackup.Common.Exceptions
{
    public class NotAuthorizedException : Exception
    {
        public NotAuthorizedException()
        {
        }

        public NotAuthorizedException(string message) : base(message)
        {
        }

        public NotAuthorizedException(string message, Exception ex) : base(message, ex)
        {
        }
    }
}
