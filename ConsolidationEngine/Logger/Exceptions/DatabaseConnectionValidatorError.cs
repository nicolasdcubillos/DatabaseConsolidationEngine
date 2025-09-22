namespace ConsolidationEngine.Logger.Exceptions
{
    public class DatabaseConnectionValidatorError : Exception
    {
        public string DatabaseName { get; }

        public DatabaseConnectionValidatorError(string message, string dbName, Exception innerException)
            : base(message, innerException)
        {
            DatabaseName = dbName;
        }
    }
}
