using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace ConsolidationEngine.ChangeTracking
{
    public class ChangeTrackingETL
    {
        private readonly string _server;
        private readonly string _originDb;
        private readonly string _targetDb;
        private readonly string _table;
        private readonly string _keyCol;
        private readonly int _batchSize;
        private readonly ILogger _logger;

        private string OriginConnection =>
            $"Server={_server};Database={_originDb};Trusted_Connection=True;TrustServerCertificate=True;";
        private string TargetConnection =>
            $"Server={_server};Database={_targetDb};Trusted_Connection=True;TrustServerCertificate=True;";

        public ChangeTrackingETL(
            string server,
            string originDb,
            string targetDb,
            string table,
            string keyCol,
            int batchSize,
            ILogger logger)
        {
            _server = server;
            _originDb = originDb;
            _targetDb = targetDb;
            _table = table;
            _keyCol = keyCol;
            _batchSize = batchSize;
            _logger = logger;
        }

        public void Run()
        {
            using var cnxOrigin = new SqlConnection(OriginConnection);
            using var cnxTarget = new SqlConnection(TargetConnection);
            cnxOrigin.Open();
            cnxTarget.Open();

            EnsureWatermarkRow(cnxTarget);

            long toVersion = GetCurrentVersion(cnxOrigin);
            long fromVersion = GetWatermark(cnxTarget);

            if (fromVersion == 0)
            {
                SetWatermark(cnxTarget, toVersion);
                _logger.LogInformation(
                    "[INIT] Watermark inicial seteado a {Version}. No se movieron datos.",
                    toVersion
                );
                return;
            }

            long minValidVersion = GetMinValidVersion(cnxOrigin);
            if (fromVersion < minValidVersion)
            {
                throw new Exception(
                    $"Watermark {fromVersion} < MIN_VALID_VERSION {minValidVersion}. Requiere reinicialización."
                );
            }

            var changes = FetchChanges(cnxOrigin, fromVersion);

            if (changes.Rows.Count == 0)
            {
                SetWatermark(cnxTarget, toVersion);
                _logger.LogInformation("[OK] Sin cambios. Watermark -> {Version}", toVersion);
                return;
            }

            var insUpd = changes.Select("SYS_CHANGE_OPERATION IN ('I','U')");
            var delRows = changes.Select("SYS_CHANGE_OPERATION = 'D'");

            UpsertBatch(cnxTarget, insUpd);
            DeleteBatch(cnxTarget, delRows);

            SetWatermark(cnxTarget, toVersion);
            _logger.LogInformation(
                "[OK] Cambios aplicados. Upserts={Upserts}, Deletes={Deletes}. Watermark -> {Version}",
                insUpd.Length,
                delRows.Length,
                toVersion
            );
        }

        private long GetCurrentVersion(SqlConnection cnx)
        {
            using var cmd = new SqlCommand("SELECT CHANGE_TRACKING_CURRENT_VERSION()", cnx);
            return (long)cmd.ExecuteScalar();
        }

        private long GetMinValidVersion(SqlConnection cnx)
        {
            using var cmd = new SqlCommand($"SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('{_table}'))", cnx);
            return (long)cmd.ExecuteScalar();
        }

        private void EnsureWatermarkRow(SqlConnection cnx)
        {
            var sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.ConsolidationEngineWatermark WHERE SourceServer=@server AND SourceDB=@db AND TableName=@table)
            INSERT INTO dbo.ConsolidationEngineWatermark (SourceServer, SourceDB, TableName, LastVersion)
            VALUES (@server, @db, @table, 0);";
            using var cmd = new SqlCommand(sql, cnx);
            cmd.Parameters.AddWithValue("@server", _server);
            cmd.Parameters.AddWithValue("@db", _originDb);
            cmd.Parameters.AddWithValue("@table", _table);
            cmd.ExecuteNonQuery();
        }

        private long GetWatermark(SqlConnection cnx)
        {
            var sql = @"SELECT LastVersion FROM dbo.ConsolidationEngineWatermark WHERE SourceServer=@server AND SourceDB=@db AND TableName=@table";
            using var cmd = new SqlCommand(sql, cnx);
            cmd.Parameters.AddWithValue("@server", _server);
            cmd.Parameters.AddWithValue("@db", _originDb);
            cmd.Parameters.AddWithValue("@table", _table);

            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt64(result) : 0;
        }

        private void SetWatermark(SqlConnection cnx, long version)
        {
            var sql = @"UPDATE dbo.ConsolidationEngineWatermark SET LastVersion=@version WHERE SourceServer=@server AND SourceDB=@db AND TableName=@table";
            using var cmd = new SqlCommand(sql, cnx);
            cmd.Parameters.AddWithValue("@version", version);
            cmd.Parameters.AddWithValue("@server", _server);
            cmd.Parameters.AddWithValue("@db", _originDb);
            cmd.Parameters.AddWithValue("@table", _table);
            cmd.ExecuteNonQuery();
        }

        private DataTable FetchChanges(SqlConnection cnx, long fromVersion)
        {
            var sql = $@"
                DECLARE @from BIGINT = @fromVersion;
                DECLARE @min BIGINT = CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('{_table}'));
                IF @from < @min
                    RAISERROR('WATERMARK_TOO_OLD', 16, 1);

                SELECT ct.SYS_CHANGE_VERSION, ct.SYS_CHANGE_OPERATION, s.*
                FROM CHANGETABLE(CHANGES {_table}, @from) AS ct
                LEFT JOIN {_table} AS s ON s.{_keyCol} = ct.{_keyCol}
                ORDER BY ct.SYS_CHANGE_VERSION, ct.{_keyCol};";

            using var cmd = new SqlCommand(sql, cnx);
            cmd.Parameters.AddWithValue("@fromVersion", fromVersion);

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private void UpsertBatch(SqlConnection cnx, DataRow[] rows)
        {
            if (rows.Length == 0) return;

            // 1. Crear tabla temporal #stage
            var cols = string.Join(",", rows[0].Table.Columns
                .Cast<DataColumn>()
                .Where(c => c.ColumnName != "SYS_CHANGE_VERSION" && c.ColumnName != "SYS_CHANGE_OPERATION")
                .Select(c => c.ColumnName));

            using (var cmd = new SqlCommand($"IF OBJECT_ID('tempdb..#stage') IS NOT NULL DROP TABLE #stage; SELECT TOP 0 {cols} INTO #stage FROM {_table};", cnx))
            {
                cmd.ExecuteNonQuery();
            }

            // 2. Preparar DataTable para bulk copy
            var tempTable = new DataTable();
            foreach (DataColumn col in rows[0].Table.Columns)
            {
                if (col.ColumnName != "SYS_CHANGE_VERSION" && col.ColumnName != "SYS_CHANGE_OPERATION")
                    tempTable.Columns.Add(col.ColumnName, col.DataType);
            }
            foreach (var row in rows)
            {
                var newRow = tempTable.NewRow();
                foreach (DataColumn col in tempTable.Columns)
                    newRow[col.ColumnName] = row[col.ColumnName] ?? DBNull.Value;
                tempTable.Rows.Add(newRow);
            }

            // 3. Bulk insert en #stage
            using (var bulkCopy = new SqlBulkCopy(cnx) { DestinationTableName = "#stage", BatchSize = _batchSize })
            {
                bulkCopy.WriteToServer(tempTable);
            }

            // 4. MERGE de #stage a la tabla final
            var setClause = string.Join(",", tempTable.Columns.Cast<DataColumn>().Where(c => c.ColumnName != _keyCol).Select(c => $"TARGET.{c.ColumnName}=SRC.{c.ColumnName}"));
            var mergeSql = $@"
                SET IDENTITY_INSERT {_table} ON;

                MERGE {_table} AS TARGET
                USING #stage AS SRC
                ON TARGET.{_keyCol} = SRC.{_keyCol}
                WHEN MATCHED THEN
                    UPDATE SET {setClause}
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT ({cols})
                    VALUES ({cols});

                SET IDENTITY_INSERT {_table} OFF;";

            using (var cmd = new SqlCommand(mergeSql, cnx))
            {
                cmd.ExecuteNonQuery();
            }

            _logger.LogInformation("[UPSERT] {Count} filas procesadas en {Table}", rows.Length, _table);
        }

        private void DeleteBatch(SqlConnection cnx, DataRow[] rows)
        {
            if (rows.Length == 0) return;

            using var cmd = new SqlCommand("", cnx);

            var keys = new List<string>();
            foreach (var row in rows)
            {
                keys.Add(row[_keyCol].ToString());
            }

            // Crear tabla temporal de keys y eliminar
            var sql = @"
            IF OBJECT_ID('tempdb..#del') IS NOT NULL DROP TABLE #del;
            CREATE TABLE #del (k NVARCHAR(40) NOT NULL);";

            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            using var bulkCopy = new SqlBulkCopy(cnx)
            {
                DestinationTableName = "#del",
                BatchSize = _batchSize
            };
            var dtKeys = new DataTable();
            dtKeys.Columns.Add("k", typeof(string));
            foreach (var k in keys)
                dtKeys.Rows.Add(k);

            bulkCopy.WriteToServer(dtKeys);

            cmd.CommandText = $"DELETE T FROM {_table} T INNER JOIN #del D ON T.{_keyCol}=D.k;";
            cmd.ExecuteNonQuery();

            _logger.LogInformation("[DELETE] {Count} filas eliminadas en {Table}", rows.Length, _table);
        }
    }
}
