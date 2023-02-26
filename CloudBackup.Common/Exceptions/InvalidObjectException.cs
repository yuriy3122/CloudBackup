
namespace CloudBackup.Common.Exceptions
{
    public class InvalidObjectException : ArgumentException
    {
        private readonly string _message;

        public InvalidObjectException()
        {
            _message = string.Empty;
        }

        public InvalidObjectException(string message) : base(message)
        {
            _message = message;
        }

        public InvalidObjectException(string message, Exception innerException) : base(message, innerException)
        {
            _message = message;
        }

        public InvalidObjectException(string message, string paramName) : base(message, paramName)
        {
            _message = message;
        }

        public InvalidObjectException(string message, string paramName, Exception innerException) : base(message, paramName, innerException)
        {
            _message = message;
        }

        public override string Message => _message;
    }
}
