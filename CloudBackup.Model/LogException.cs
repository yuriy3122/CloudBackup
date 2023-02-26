namespace CloudBackup.Model
{
    public static class LogException
    {
        public static Log ToLogEntry(this Exception exception)
        {
            var logEntity = new Model.Log()
            {
                EventDate = DateTime.UtcNow,
                MessageText = exception.Message,
                XmlData = exception.ToString(),
                Severity = Severity.Error,
                ObjectType = exception.GetType().ToString(),
                ObjectId = string.Empty
            };

            return logEntity;
        }
    }
}