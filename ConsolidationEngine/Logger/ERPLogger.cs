using ConsolidationEngine.Repository;
using Microsoft.Extensions.Logging;

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

        public static void Log(
            LogLevel level,
            string message,
            string sourceDatabase,
            string targetDatabase,
            string tableName = null,
            string payload = null
        )
        {
            var data = new Dictionary<string, object>
            {
                { "LogLevel", level.ToString() },
                { "Message", message },
                { "SourceDatabase", sourceDatabase },
                { "TargetDatabase", targetDatabase },
                { "TableName", (object?)tableName ?? DBNull.Value },
                { "Payload", (object?)payload ?? DBNull.Value },
                { "CreatedAt", DateTime.UtcNow }
            };

            SqlRepository.InsertAsync("ConsolidationEngineLogs", data, targetDatabase).GetAwaiter().GetResult();
        }
    }
}
