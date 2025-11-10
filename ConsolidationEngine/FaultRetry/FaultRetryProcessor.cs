using ConsolidationEngine.Config;
using ConsolidationEngine.Repository;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsolidationEngine.FaultRetry
{
    public class FaultRetryProcessor
    {
        private readonly ConsolidationSettings _settings;
        private readonly ILogger _logger;

        public FaultRetryProcessor(ConsolidationSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta el proceso de reintentos para todas las bases de datos destino.
        /// </summary>
        public async Task RunForAllTargetsAsync()
        {
            var targetDbs = GetTargetDatabases();

            if (targetDbs.Count == 0)
            {
                _logger.LogWarning("[FaultRetryProcessor] No target databases found in settings.");
                return;
            }

            foreach (var db in targetDbs)
            {
                try
                {
                    _logger.LogInformation("[FaultRetryProcessor] Checking retries for target DB: {database}", db);
                    await ProcessRetriesAsync(db);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FaultRetryProcessor] Error processing retries for {database}", db);
                }
            }
        }

        /// <summary>
        /// Procesa los reintentos para una base de datos específica.
        /// </summary>
        private async Task ProcessRetriesAsync(string database)
        {
            try
            {
                var errors = await SqlRepository.SelectAsync(
                    tableName: "ConsolidationEngineErrors",
                    database: database,
                    whereClause: "Retry = 1"
                );

                if (errors.Count == 0)
                {
                    _logger.LogInformation("[FaultRetryProcessor] No retryable errors in {database}", database);
                    return;
                }

                _logger.LogInformation("[FaultRetryProcessor] Found {count} retryable errors in {database}", errors.Count, database);

                foreach (var error in errors)
                {
                    try
                    {
                        await RetryRecordAsync(error, database);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FaultRetryProcessor] Error retrying record ID={id} in {database}", error.GetValueOrDefault("Id"), database);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FaultRetryProcessor] General error while processing retries in {database}", database);
            }
        }

        /// <summary>
        /// Proceso de reintento
        /// </summary>
        private async Task RetryRecordAsync(Dictionary<string, object> error, string database)
        {
            var id = error.GetValueOrDefault("Id");
            var sourceKey = error.GetValueOrDefault("SourceKey")?.ToString()?.Trim();
            var tableName = error.GetValueOrDefault("TableName")?.ToString()?.Trim();
            var payloadSql = error.GetValueOrDefault("Payload")?.ToString()?.Trim();

            _logger.LogInformation("[FaultRetryProcessor] Retrying record Id={id} (SourceKey={sourceKey}) in {database}", id, sourceKey, database);

            if (string.IsNullOrEmpty(payloadSql))
            {
                _logger.LogWarning("[FaultRetryProcessor] No SQL payload found for ErrorId={id}", id);
                return;
            }

            try
            {
                using var cnx = SqlConnectionBuilder.Instance.CreateConnection(database);
                await cnx.OpenAsync();

                using var cmd = new SqlCommand(payloadSql, cnx)
                {
                    CommandTimeout = 120 // opcional, por si el query tarda
                };

                var rows = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("[FaultRetryProcessor] Retry successful for ErrorId={id} (RowsAffected={rows})", id, rows);

                // Marcar el registro como reintentado
                var updateRetrySql = @"
                    UPDATE dbo.ConsolidationEngineErrors
                    SET Retry = 2,
                        RetryCount = RetryCount + 1
                    WHERE Id = @Id;";

                using var updateCmd = new SqlCommand(updateRetrySql, cnx);
                updateCmd.Parameters.AddWithValue("@Id", id);
                await updateCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FaultRetryProcessor] Retry failed for ErrorId={id} in {database}", id, database);
            }

            _logger.LogInformation("[FaultRetryProcessor] Retry completed for ErrorId={id} in {database}", id, database);
        }


        /// <summary>
        /// Extrae la lista única de bases de datos destino desde los settings.
        /// </summary>
        private List<string> GetTargetDatabases()
        {
            var targetDbs = new HashSet<string>(
                _settings.Databases.Select(db => db.Target),
                StringComparer.OrdinalIgnoreCase
            );

            return targetDbs.ToList();
        }
    }

    internal static class DictionaryExtensions
    {
        public static object? GetValueOrDefault(this Dictionary<string, object> dict, string key)
            => dict.ContainsKey(key) ? dict[key] : null;
    }
}
