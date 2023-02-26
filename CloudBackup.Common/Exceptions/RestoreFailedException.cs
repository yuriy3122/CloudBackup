
namespace CloudBackup.Common.Exceptions
{
    public class RestoreFailedException : Exception
    {
        public RestoreFailedException()
        {
        }

        public RestoreFailedException(string message)
            : base(message)
        {
        }
    }
}