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
    /// <summary>
    /// Procesador de reintentos para errores de consolidación registrados en ConsolidationEngineErrors.
    /// 
    /// Estados del campo Retry:
    /// - 0: Error que NO será reintentado (inicial, permanente, o excedió límite de reintentos)
    /// - 1: Error marcado para reintento (el FaultRetryProcessor lo procesará)
    /// - 2: Reintento completado exitosamente
    /// 
    /// Flujo de reintentos:
    /// 1. Los errores se registran con Retry = 0 inicialmente
    /// 2. Pueden ser marcados manualmente como Retry = 1 para reintento
    /// 3. Si el reintento es exitoso → Retry = 2
    /// 4. Si el reintento falla y RetryCount < MaxRetryAttempts → permanece en Retry = 1, incrementa RetryCount
    /// 5. Si el reintento falla y RetryCount >= MaxRetryAttempts → Retry = 0 (no se reintentará más)
    /// </summary>
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
        /// Busca registros con Retry = 1 en ConsolidationEngineErrors y los reintenta.
        /// </summary>
        public async Task RunForAllTargetsAsync()
        {
            var targetDbs = GetTargetDatabases();

            if (targetDbs.Count == 0)
            {
                _logger.LogWarning("[FaultRetryProcessor] No se encontraron bases de datos destino en la configuración.");
                return;
            }

            foreach (var db in targetDbs)
            {
                try
                {
                    _logger.LogInformation("[FaultRetryProcessor] Verificando reintentos para BD destino: {database}", db);
                    await ProcessRetriesAsync(db);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FaultRetryProcessor] Error procesando reintentos para {database}", db);
                }
            }
        }

        /// <summary>
        /// Procesa los reintentos para una base de datos específica.
        /// Selecciona registros con Retry = 1 (pendientes de reintento).
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
                    _logger.LogInformation("[FaultRetryProcessor] No hay errores para reintentar en {database}", database);
                    return;
                }

                _logger.LogInformation("[FaultRetryProcessor] Se encontraron {count} errores para reintentar en {database}", errors.Count, database);

                foreach (var error in errors)
                {
                    try
                    {
                        await RetryRecordAsync(error, database);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FaultRetryProcessor] Error reintentando registro ID={id} en {database}", error.GetValueOrDefault("Id"), database);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FaultRetryProcessor] Error general procesando reintentos en {database}", database);
            }
        }

        /// <summary>
        /// Proceso de reintento de un registro individual.
        /// Si el reintento es exitoso, actualiza Retry = 2 (reintento completado).
        /// Si falla y no ha excedido el límite de reintentos, permanece en Retry = 1 e incrementa RetryCount.
        /// Si falla y excede el límite, se marca como Retry = 0 (no se reintentará más).
        /// </summary>
        private async Task RetryRecordAsync(Dictionary<string, object> error, string database)
        {
            var id = error.GetValueOrDefault("Id");
            var sourceKey = error.GetValueOrDefault("SourceKey")?.ToString()?.Trim();
            var tableName = error.GetValueOrDefault("TableName")?.ToString()?.Trim();
            var payloadSql = error.GetValueOrDefault("Payload")?.ToString()?.Trim();
            var retryCount = error.GetValueOrDefault("RetryCount") != null 
                ? Convert.ToInt32(error.GetValueOrDefault("RetryCount")) 
                : 0;

            _logger.LogInformation("[FaultRetryProcessor] Reintentando registro Id={id} (SourceKey={sourceKey}, RetryCount={retryCount}/{maxRetries}) en {database}", 
                id, sourceKey, retryCount, _settings.MaxRetryAttempts, database);

            if (string.IsNullOrEmpty(payloadSql))
            {
                _logger.LogWarning("[FaultRetryProcessor] No se encontró payload SQL para ErrorId={id}", id);
                return;
            }

            // Verificar si ya excedió el límite de reintentos
            if (retryCount >= _settings.MaxRetryAttempts)
            {
                _logger.LogWarning(
                    "[FaultRetryProcessor] ErrorId={id} (SourceKey={sourceKey}) excedió el límite máximo de reintentos ({retryCount}/{maxRetries}). " +
                    "Marcando como Retry = 0 (error permanente). Se requiere intervención manual.",
                    id, sourceKey, retryCount, _settings.MaxRetryAttempts);

                await MarkAsNonRetryableAsync(id, database);
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

                using (var dateCmd = new SqlCommand("SET DATEFORMAT ymd;", cnx))
                    dateCmd.ExecuteNonQuery();

                var rows = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("[FaultRetryProcessor] Reintento exitoso para ErrorId={id} (FilasAfectadas={rows})", id, rows);

                // Marcar el registro como reintentado exitosamente (Retry = 2)
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
                _logger.LogError(ex, "[FaultRetryProcessor] Reintento fallido para ErrorId={id} (RetryCount={retryCount}) en {database}", 
                    id, retryCount, database);
                
                // Incrementar el contador de reintentos
                await IncrementRetryCountAsync(id, database, retryCount);
                
                // Si ahora excede el límite, marcar como Retry = 0 (no se reintentará más)
                if (retryCount + 1 >= _settings.MaxRetryAttempts)
                {
                    _logger.LogWarning(
                        "[FaultRetryProcessor] ErrorId={id} (SourceKey={sourceKey}) alcanzó el límite máximo de reintentos después de este fallo. " +
                        "Marcando como Retry = 0 (no se reintentará más).",
                        id, sourceKey);
                    
                    await MarkAsNonRetryableAsync(id, database);
                }
                // Si no excede el límite, permanece en Retry = 1 para futuros intentos
            }
        }

        /// <summary>
        /// Incrementa el contador de reintentos sin cambiar el estado Retry.
        /// </summary>
        private async Task IncrementRetryCountAsync(object id, string database, int currentRetryCount)
        {
            try
            {
                using var cnx = SqlConnectionBuilder.Instance.CreateConnection(database);
                await cnx.OpenAsync();

                var updateSql = @"
                    UPDATE dbo.ConsolidationEngineErrors
                    SET RetryCount = RetryCount + 1
                    WHERE Id = @Id;";

                using var updateCmd = new SqlCommand(updateSql, cnx);
                updateCmd.Parameters.AddWithValue("@Id", id);
                await updateCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("[FaultRetryProcessor] RetryCount incrementado a {newCount} para ErrorId={id}", 
                    currentRetryCount + 1, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FaultRetryProcessor] Error al incrementar RetryCount para ErrorId={id}", id);
            }
        }

        /// <summary>
        /// Marca un error como no reintentar más (Retry = 0) cuando excede el límite de reintentos.
        /// </summary>
        private async Task MarkAsNonRetryableAsync(object id, string database)
        {
            try
            {
                using var cnx = SqlConnectionBuilder.Instance.CreateConnection(database);
                await cnx.OpenAsync();

                var updateSql = @"
                    UPDATE dbo.ConsolidationEngineErrors
                    SET Retry = 0
                    WHERE Id = @Id;";

                using var updateCmd = new SqlCommand(updateSql, cnx);
                updateCmd.Parameters.AddWithValue("@Id", id);
                await updateCmd.ExecuteNonQueryAsync();

                _logger.LogWarning("[FaultRetryProcessor] ErrorId={id} marcado como Retry = 0 (no se reintentará más)", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FaultRetryProcessor] Error al marcar ErrorId={id} como no reintentar", id);
            }
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
