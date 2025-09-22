using ConsolidationEngine.Repository;

namespace ConsolidationEngine.ChangeTracking
{
    public class ChangeTrackingETL
    {
        private readonly SqlConsolidationHelper sqlConsolidationHelper;
        private readonly ILogger _logger;
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
                _logger.LogInformation(
                    "[CHANGE TRACKING ETL] INIT: Watermark inicial seteado a {Version}. No se movieron datos.",
                    toVersion
                );
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

            // Fetching changes

            var changes = sqlConsolidationHelper.FetchChanges(cnxOrigin, fromVersion, toVersion);

            if (changes.Rows.Count == 0)
            {
                sqlConsolidationHelper.SetWatermark(cnxTarget, toVersion);
                /*_logger.LogInformation(
                    "[OK] {Origin}->{Target} Tabla={Table} | Sin cambios. Watermark -> {Version}",
                    _originDb, _targetDb, _table, toVersion
                );*/
                return;
            }

            // Upsert

            var insUpd = changes.Select("SYS_CHANGE_OPERATION IN ('I','U')");
            int rowsUpserted = sqlConsolidationHelper.UpsertBatchWithFallback(cnxTarget, insUpd);
            if (rowsUpserted != 0 && rowsUpserted != insUpd.Length)
            {
                _logger.LogWarning("[CHANGE TRACKING ETL] UPSERT: Se esperaba procesar {Expected} filas pero MERGE afectó {Actual}", insUpd.Length, rowsUpserted);
            }

            // Delete

            var delRows = changes.Select("SYS_CHANGE_OPERATION = 'D'");
            int rowsDeleted = sqlConsolidationHelper.DeleteBatch(cnxTarget, delRows);
            if (rowsDeleted != 0 && rowsDeleted != delRows.Length)
            {
                _logger.LogWarning("[CHANGE TRACKING ETL] DELETE: Se esperaba procesar {Expected} filas pero afectó {Actual}", delRows.Length, rowsDeleted);
            }

            sqlConsolidationHelper.SetWatermark(cnxTarget, toVersion);

            _logger.LogInformation(
                "[CHANGE TRACKING ETL] DELETE: {Origin}->{Target} Tabla={Table} | Cambios aplicados. Upserts={Upserts}, Deletes={Deletes}, Watermark -> {Version}",
                _originDb, _targetDb, _table, rowsUpserted, rowsDeleted, toVersion
            );
        }
    }
}
