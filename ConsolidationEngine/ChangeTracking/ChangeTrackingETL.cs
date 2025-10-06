using ConsolidationEngine.Repository;
using ConsolidationEngine.Logger;

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
            int batchSize,
            ILogger logger)
        {
            sqlConsolidationHelper = new SqlConsolidationHelper(server, originDb, targetDb, table, keyCol, batchSize, logger);
            _logger = logger;
            _dualLogger = new DualLogger(logger);
            _originDb = originDb;
            _targetDb = targetDb;
            _table = table;
        }

        public void Run()
        {
            // Creating connections

            using var cnxOrigin = SqlConnectionBuilder.Instance.CreateConnection(_originDb);
            using var cnxTarget = SqlConnectionBuilder.Instance.CreateConnection(_targetDb);

            cnxOrigin.Open();
            cnxTarget.Open();

            sqlConsolidationHelper.EnsureWatermarkRow(cnxTarget);

            // Get From - To versions

            long toVersion = sqlConsolidationHelper.GetCurrentVersion(cnxOrigin);
            long fromVersion = sqlConsolidationHelper.GetWatermark(cnxTarget);

            if (fromVersion == 0 && toVersion > 0)
            {
                sqlConsolidationHelper.SetWatermark(cnxTarget, toVersion);
                _dualLogger.Log(LogLevel.Information, $"[CHANGE TRACKING ETL] INIT: Watermark inicial seteado a {toVersion}. No se movieron datos.", _originDb, _targetDb, _table);
                return;
            }
            
            // Checking min version

            long minValidVersion = sqlConsolidationHelper.GetMinValidVersion(cnxOrigin);
            if (fromVersion < minValidVersion)
            {
                throw new Exception(
                    $"[CHANGE TRACKING ETL] WATERMARK MISMATCH: Watermark {fromVersion} < MIN_VALID_VERSION {minValidVersion}. Requiere reinicialización. Origen={_originDb}, Destino={_targetDb}, Tabla={_table}"
                );
            }

            if (fromVersion == toVersion)
            {
                _dualLogger.Log(LogLevel.Information, $"[OK] {_originDb}->{_targetDb} Tabla={_table} | Sin cambios. Watermark -> {toVersion}", _originDb, _targetDb, _table);
                return;
            }

            // Fetching changes

            _dualLogger.Log(LogLevel.Information, $"[CHANGE TRACKING ETL] Procesando cambios de {_originDb} hacia {_targetDb} para la tabla {_table} (fromVersion={fromVersion}, toVersion={toVersion})", _originDb, _targetDb, _table);

            var changes = sqlConsolidationHelper.FetchChanges(cnxOrigin, fromVersion, toVersion);

            if (changes.Rows.Count == 0)
            {
                sqlConsolidationHelper.SetWatermark(cnxTarget, toVersion);
                _dualLogger.Log(LogLevel.Information, $"[OK] {_originDb}->{_targetDb} Tabla={_table} | Sin cambios. Watermark -> {toVersion}", _originDb, _targetDb, _table);
                return;
            }
            
            // Upsert

            var insUpd = changes.Select("SYS_CHANGE_OPERATION IN ('I','U')");
            int rowsUpserted = sqlConsolidationHelper.UpsertBatchWithFallback(cnxTarget, insUpd);
            if (rowsUpserted != 0 && rowsUpserted != insUpd.Length)
            {
                _dualLogger.Log(LogLevel.Warning, $"[CHANGE TRACKING ETL] UPSERT: Se esperaba procesar {insUpd.Length} filas pero MERGE afectó {rowsUpserted}", _originDb, _targetDb, _table);
            }

            // Delete

            var delRows = changes.Select("SYS_CHANGE_OPERATION = 'D'");
            int rowsDeleted = sqlConsolidationHelper.DeleteBatch(cnxTarget, delRows);
            if (rowsDeleted != 0 && rowsDeleted != delRows.Length)
            {
                _dualLogger.Log(LogLevel.Warning, $"[CHANGE TRACKING ETL] DELETE: Se esperaba procesar {delRows.Length} filas pero afectó {rowsDeleted}", _originDb, _targetDb, _table);
            }

            sqlConsolidationHelper.SetWatermark(cnxTarget, toVersion);
        }
    }
}
