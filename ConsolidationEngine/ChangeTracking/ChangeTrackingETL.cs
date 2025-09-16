using ConsolidationEngine.Repository;

namespace ConsolidationEngine.ChangeTracking
{
    public class ChangeTrackingETL
    {
        private readonly SqlRepository _repository;
        private readonly ILogger _logger;

        public ChangeTrackingETL(
            string server,
            string originDb,
            string targetDb,
            string originConnectionString,
            string targetConnectionString,
            string table,
            string keyCol,
            int batchSize,
            ILogger logger)
        {
            _repository = new SqlRepository(server, originDb, targetDb, originConnectionString, targetConnectionString, table, keyCol, batchSize, logger);
            _logger = logger;
        }

        public void Run()
        {
            using var cnxOrigin = _repository.CreateOriginConnection();
            using var cnxTarget = _repository.CreateTargetConnection();
            cnxOrigin.Open();
            cnxTarget.Open();

            _repository.EnsureWatermarkRow(cnxTarget);

            long toVersion = _repository.GetCurrentVersion(cnxOrigin);
            long fromVersion = _repository.GetWatermark(cnxTarget);

            if (fromVersion == 0)
            {
                _repository.SetWatermark(cnxTarget, toVersion);
                _logger.LogInformation(
                    "[INIT] Watermark inicial seteado a {Version}. No se movieron datos.",
                    toVersion
                );
                return;
            }

            long minValidVersion = _repository.GetMinValidVersion(cnxOrigin);
            if (fromVersion < minValidVersion)
            {
                throw new Exception(
                    $"Watermark {fromVersion} < MIN_VALID_VERSION {minValidVersion}. Requiere reinicialización."
                );
            }

            var changes = _repository.FetchChanges(cnxOrigin, fromVersion, toVersion);

            if (changes.Rows.Count == 0)
            {
                _repository.SetWatermark(cnxTarget, toVersion);
                _logger.LogInformation("[OK] Sin cambios. Watermark -> {Version}", toVersion);
                return;
            }

            var insUpd = changes.Select("SYS_CHANGE_OPERATION IN ('I','U')");
            var delRows = changes.Select("SYS_CHANGE_OPERATION = 'D'");

            _repository.UpsertBatch(cnxTarget, insUpd);
            _repository.DeleteBatch(cnxTarget, delRows);

            _repository.SetWatermark(cnxTarget, toVersion);
            _logger.LogInformation(
                "[OK] Cambios aplicados. Upserts={Upserts}, Deletes={Deletes}. Watermark -> {Version}",
                insUpd.Length,
                delRows.Length,
                toVersion
            );
        }
    }
}
