using ConsolidationEngine.Repository;
using ConsolidationEngine.Logger;
using Microsoft.Data.SqlClient;

namespace ConsolidationEngine.ChangeTracking
{
    public class ChangeTrackingETL
    {
        private readonly SqlConsolidationHelper sqlConsolidationHelper;
        private readonly ILogger _logger;
        private readonly DualLogger _dualLogger;
        private readonly string _originDb;
        private readonly string _targetDb;
        private readonly string _table;

        public ChangeTrackingETL(
            string server,
            string originDb,
            string targetDb,
            string user,
            string password,
            string table,
            string keyCol,
            bool skipPrimaryKey,
            int batchSize,
            int upsertBatchWithFallbackTimeoutSeconds,
            ILogger logger,
            string rowFilter = null)
        {
            sqlConsolidationHelper = new SqlConsolidationHelper(
                server, originDb, targetDb, table, keyCol,
                batchSize, skipPrimaryKey, upsertBatchWithFallbackTimeoutSeconds,
                logger, rowFilter);
            _logger = logger;
            _dualLogger = new DualLogger(logger);
            _originDb = originDb;
            _targetDb = targetDb;
            _table = table;
        }

        public void Run()
        {
            SqlConnection cnxOrigin = null;
            SqlConnection cnxTarget = null;

            try
            {
                // Creating connections
                cnxOrigin = SqlConnectionBuilder.Instance.CreateConnection(_originDb);
                cnxTarget = SqlConnectionBuilder.Instance.CreateConnection(_targetDb);

                cnxOrigin.Open();
                cnxTarget.Open();

                sqlConsolidationHelper.EnsureWatermarkRow(cnxTarget);

                // Get From - To versions
                long toVersion = sqlConsolidationHelper.GetCurrentVersion(cnxOrigin);
                long fromVersion = sqlConsolidationHelper.GetWatermark(cnxTarget);

                // Initialize watermark on first run ONLY when the origin DB has no
                // trackable history yet (toVersion == 0). In that case there is
                // nothing to replicate, so we just record the starting point and exit.
                // When toVersion > 0 the origin already has change history, so we fall
                // through to the normal processing flow below and replicate everything
                // that is available — this prevents the silent data loss that occurred
                // when the motor started against a DB that already had tracked changes.
                if (fromVersion == 0 && toVersion == 0)
                {
                    //_dualLogger.Log(LogLevel.Information, $"[CHANGE TRACKING ETL] INIT: Change Tracking aún sin versiones en origen. Watermark queda en 0.", _originDb, _targetDb, _table);
                    return;
                }

                // Checking min version - CRITICAL CHECK
                // Must be evaluated before FetchChanges to avoid WATERMARK_TOO_OLD error.
                // fromVersion == 0 on first run is always valid (CHANGETABLE accepts 0).
                long minValidVersion = sqlConsolidationHelper.GetMinValidVersion(cnxOrigin);
                if (fromVersion > 0 && fromVersion < minValidVersion)
                {
                    _dualLogger.Log(
                        LogLevel.Error,
                        $"[CHANGE TRACKING ETL] WATERMARK MISMATCH: Watermark {fromVersion} < MIN_VALID_VERSION {minValidVersion}. " +
                        $"Se perdieron datos de tracking. Actualizando watermark a {toVersion} para continuar. " +
                        $"ACCIÓN REQUERIDA: Validar datos manualmente o ejecutar reinicialización completa. Origen={_originDb}, Destino={_targetDb}, Tabla={_table}",
                        _originDb,
                        _targetDb,
                        _table
                    );

                    // Actualizamos el watermark para no bloquear el sistema, pero loggeamos el error crítico
                    // El FaultRetryProcessor no puede recuperar estos datos perdidos - se requiere intervención manual
                    sqlConsolidationHelper.SetWatermark(cnxTarget, toVersion);
                    return;
                }

                // No changes
                if (fromVersion == toVersion)
                {
                    return;
                }

                // Fetching changes
                _dualLogger.Log(LogLevel.Information, $"[CHANGE TRACKING ETL] Procesando cambios de {_originDb} hacia {_targetDb} para la tabla {_table} (fromVersion={fromVersion}, toVersion={toVersion})", _originDb, _targetDb, _table);

                var fetchResult = sqlConsolidationHelper.FetchChanges(cnxOrigin, fromVersion, toVersion);

                // Si hubieron cambios en el rango pero todos fueron excluidos por el RowFilter,
                // avanzamos el watermark igualmente (comportamiento correcto e intencional)
                // y loggeamos explícitamente para no confundir con "sin cambios reales".
                if (fetchResult.ChangedRows.Rows.Count == 0)
                {
                    sqlConsolidationHelper.SetWatermark(cnxTarget, toVersion);

                    if (fetchResult.HasFilteredOut)
                    {
                        _dualLogger.Log(
                            LogLevel.Information,
                            $"[OK] {_originDb}->{_targetDb} Tabla={_table} | {fetchResult.FilteredOutCount} cambio(s) excluidos por RowFilter (no aplican la condición). Watermark -> {toVersion}",
                            _originDb, _targetDb, _table);
                    }
                    else
                    {
                        _dualLogger.Log(LogLevel.Information, $"[OK] {_originDb}->{_targetDb} Tabla={_table} | Sin cambios detectados. Watermark -> {toVersion}", _originDb, _targetDb, _table);
                    }
                    return;
                }

                var changes = fetchResult.ChangedRows;

                // Process Upserts
                var insUpd = changes.Select("SYS_CHANGE_OPERATION IN ('I','U')");
                int rowsUpserted = 0;
                
                if (insUpd.Length > 0)
                {
                    // UpsertBatchWithFallback maneja su propia conexión/transacción
                    // Los errores individuales se loggean en ConsolidationEngineErrors para retry posterior
                    rowsUpserted = sqlConsolidationHelper.UpsertBatchWithFallback(_targetDb, insUpd);
                    
                    // Validar: si hay errores parciales, se loggean pero continuamos
                    if (rowsUpserted < insUpd.Length)
                    {
                        _dualLogger.Log(
                            LogLevel.Warning, 
                            $"[CHANGE TRACKING ETL] UPSERT: Se procesaron {rowsUpserted}/{insUpd.Length} filas correctamente. " +
                            $"Los errores fueron registrados para retry posterior por FaultRetryProcessor.", 
                            _originDb, 
                            _targetDb, 
                            _table
                        );
                    }
                }

                // Process Deletes
                var delRows = changes.Select("SYS_CHANGE_OPERATION = 'D'");
                int rowsDeleted = 0;
                
                if (delRows.Length > 0)
                {
                    rowsDeleted = sqlConsolidationHelper.DeleteBatch(cnxTarget, delRows);
                    
                    // Menos filas eliminadas puede ser normal (registros ya no existen)
                    if (rowsDeleted < delRows.Length)
                    {
                        _dualLogger.Log(
                            LogLevel.Information, 
                            $"[CHANGE TRACKING ETL] DELETE: Se eliminaron {rowsDeleted}/{delRows.Length} filas. " +
                            $"Algunas filas pueden no existir en destino (comportamiento esperado).", 
                            _originDb, 
                            _targetDb, 
                            _table
                        );
                    }
                }

                // Update watermark - Siempre se actualiza para no bloquear el flujo
                // Los errores individuales ya fueron registrados para retry
                sqlConsolidationHelper.SetWatermark(cnxTarget, toVersion);

                var filteredOutMsg = fetchResult.HasFilteredOut
                    ? $" | {fetchResult.FilteredOutCount} excluidos por RowFilter"
                    : string.Empty;

                _dualLogger.Log(
                    LogLevel.Information, 
                    $"[OK] {_originDb}->{_targetDb} Tabla={_table} | Procesados: {rowsUpserted}/{insUpd.Length} upserts, {rowsDeleted}/{delRows.Length} deletes{filteredOutMsg} | Watermark -> {toVersion}", 
                    _originDb, 
                    _targetDb, 
                    _table
                );
            }
            catch (Exception ex)
            {
                _dualLogger.Log(
                    LogLevel.Error,
                    $"[CHANGE TRACKING ETL] ERROR: {ex.Message}",
                    _originDb,
                    _targetDb,
                    _table
                );
                _logger.LogError(ex, "[CHANGE TRACKING ETL] Error crítico en Run(). El watermark NO se actualizó. Origen={OriginDb}, Destino={TargetDb}, Tabla={Table}", _originDb, _targetDb, _table);
                
                // En caso de error crítico NO actualizamos watermark
                // El proceso reintentará desde la misma posición en el próximo ciclo
                throw;
            }
            finally
            {
                // Asegurar cierre de recursos
                cnxTarget?.Close();
                cnxTarget?.Dispose();
                cnxOrigin?.Close();
                cnxOrigin?.Dispose();
            }
        }
    }
}
