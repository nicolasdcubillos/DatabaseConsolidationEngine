using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidationEngine.Logger
{
    internal class DualLogger
    {
        private readonly ILogger _logger;

        public DualLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void LogError(string sourceKey, string sourceDatabase, string tableName, string operation, string errorMessage, string errorDetails, string payload, string retryCount, string targetDatabase)
        {
            _logger?.Log(LogLevel.Error, "{SourceKey} | {SourceDatabase} | {TableName} | {Operation} | {ErrorMessage} | {ErrorDetails} | {Payload} | {RetryCount}",
                sourceKey, sourceDatabase, tableName, operation, errorMessage, errorDetails, payload, retryCount);

            ERPLogger.LogError(sourceKey, sourceDatabase, tableName, operation, errorMessage, errorDetails, payload, retryCount, targetDatabase);
        }

        public void Log(LogLevel level, string message, string sourceDatabase, string targetDatabase, string tableName = null, string payload = null)
        {
            _logger?.Log(level, message, sourceDatabase, targetDatabase, tableName, payload);
            ERPLogger.Log(level, message, sourceDatabase, targetDatabase, tableName, payload);
        }
    }
}
