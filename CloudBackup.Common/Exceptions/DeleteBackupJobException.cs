using System;

namespace CloudBackup.Common.Exceptions
{
    public class DeleteBackupJobException : Exception
    {
        public DeleteBackupJobException()
        {
        }

        public DeleteBackupJobException(string message)
            : base(message)
        {
        }
    }
}