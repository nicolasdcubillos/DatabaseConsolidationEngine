using ConsolidationEngine.Repository;

namespace ConsolidationEngine.Logger
{
    public static class ERPLogger
    {
        public static void LogError(
            string sourceKey,
            string sourceDatabase,
            string tableName,
            string operation,
            string errorMessage,
            string errorDetails,
            string payload,
            string retryCount,
            string targetDatabase
        )
        {
            var data = new Dictionary<string, object>
            {
                { "SourceKey", (object?)sourceKey ?? DBNull.Value },
                { "SourceDatabase", (object?)sourceDatabase ?? DBNull.Value },
                { "TableName", tableName },
                { "Operation", operation },
                { "ErrorMessage", errorMessage },
                { "ErrorDetails", (object?)errorDetails ?? DBNull.Value },
                { "Payload", (object?)payload ?? DBNull.Value },
                { "RetryCount", retryCount },
                { "CreatedAt", DateTime.UtcNow }
            };

            SqlRepository.InsertAsync("ConsolidationEngineErrors", data, targetDatabase).GetAwaiter().GetResult();
        }
    }
}
