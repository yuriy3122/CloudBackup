using System;

namespace CloudBackup.Common.Exceptions
{
    public class DisabledUserException : Exception
    {
        public DisabledUserException() : this("User is disabled.")
        {
        }

        public DisabledUserException(string message)
            : base(message)
        {
        }

        public DisabledUserException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
