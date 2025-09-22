using ConsolidationEngine.Logger;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidationEngine.Repository
{
    public class SqlConsolidationHelper
    {
        private readonly string _server;
        private readonly string _originDb;
        private readonly string _targetDb;
        private readonly string _table;
        private readonly string _keyCol;
        private readonly int _batchSize;
        private readonly ILogger _logger;
        private readonly LoggerDecorator _loggerDecorator;

        public SqlConsolidationHelper(
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
            _loggerDecorator = new LoggerDecorator(logger);
        }

        public long GetCurrentVersion(SqlConnection cnx)
        {
            using var cmd = new SqlCommand("SELECT CHANGE_TRACKING_CURRENT_VERSION()", cnx);
            return (long)cmd.ExecuteScalar();
        }

        public long GetMinValidVersion(SqlConnection cnx)
        {
            using var cmd = new SqlCommand($"SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('{_table}'))", cnx);
            return (long)cmd.ExecuteScalar();
        }

        public void EnsureWatermarkRow(SqlConnection cnx)
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

        public long GetWatermark(SqlConnection cnx)
        {
            var sql = @"SELECT LastVersion FROM dbo.ConsolidationEngineWatermark WHERE SourceServer=@server AND SourceDB=@db AND TableName=@table";
            using var cmd = new SqlCommand(sql, cnx);
            cmd.Parameters.AddWithValue("@server", _server);
            cmd.Parameters.AddWithValue("@db", _originDb);
            cmd.Parameters.AddWithValue("@table", _table);

            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt64(result) : 0;
        }

        public void SetWatermark(SqlConnection cnx, long version)
        {
            var sql = @"UPDATE dbo.ConsolidationEngineWatermark SET LastVersion=@version WHERE SourceServer=@server AND SourceDB=@db AND TableName=@table";
            using var cmd = new SqlCommand(sql, cnx);
            cmd.Parameters.AddWithValue("@version", version);
            cmd.Parameters.AddWithValue("@server", _server);
            cmd.Parameters.AddWithValue("@db", _originDb);
            cmd.Parameters.AddWithValue("@table", _table);
            cmd.ExecuteNonQuery();
        }

        public DataTable FetchChanges(SqlConnection cnx, long fromVersion, long toVersion)
        {
            var sql = $@"
                DECLARE @from BIGINT = @fromVersion;
                DECLARE @min BIGINT = CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('{_table}'));
                IF @from < @min
                    RAISERROR('WATERMARK_TOO_OLD', 16, 1);

                SELECT ct.SYS_CHANGE_VERSION, ct.SYS_CHANGE_OPERATION, s.*
                FROM CHANGETABLE(CHANGES {_table}, @from) AS ct
                LEFT JOIN {_table} AS s ON s.{_keyCol} = ct.{_keyCol}
                WHERE ct.SYS_CHANGE_VERSION <= {toVersion}
                ORDER BY ct.SYS_CHANGE_VERSION, ct.{_keyCol};";

            using var cmd = new SqlCommand(sql, cnx);
            cmd.Parameters.AddWithValue("@fromVersion", fromVersion);

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public int UpsertBatchWithFallback(SqlConnection cnx, DataRow[] rows)
        {
            int rowsAffected = 0;

            if (rows.Length == 0) return 0;

            // Columnas de la tabla sin tracking

            var allCols = rows[0].Table.Columns
                .Cast<DataColumn>()
                .Where(c => c.ColumnName != "SYS_CHANGE_VERSION" && c.ColumnName != "SYS_CHANGE_OPERATION")
                .Select(c => c.ColumnName)
                .ToList();

            // Agregar columna SourceKey

            if (!allCols.Contains("SourceKey"))
                allCols.Add("SourceKey");

            // Columnas sin PK para insert

            var insertCols = string.Join(",", allCols.Where(c => c != _keyCol));
            var insertValues = string.Join(",", allCols.Where(c => c != _keyCol).Select(c => $"SRC.{c}"));

            // Columnas completas para crear #stage

            var colsForStage = string.Join(",", allCols);

            // Crear tabla temporal

            using (var cmd = new SqlCommand($@"
            IF OBJECT_ID('tempdb..#stage') IS NOT NULL DROP TABLE #stage;
            SELECT TOP 0 {colsForStage} INTO #stage FROM {_table};", cnx))
            {
                cmd.ExecuteNonQuery();
            }

            // Crear DataTable temporal

            var tempTable = new DataTable();
            foreach (var colName in allCols)
                tempTable.Columns.Add(colName, typeof(object));

            foreach (var row in rows)
            {
                var newRow = tempTable.NewRow();
                foreach (var colName in allCols.Where(c => c != "SourceKey"))
                    newRow[colName] = row[colName] ?? DBNull.Value;

                // Generar SourceKey
                newRow["SourceKey"] = $"{_originDb}_{row[_keyCol]}";

                tempTable.Rows.Add(newRow);
            }

            // Bulk insert a #stage

            using (var bulkCopy = new SqlBulkCopy(cnx) { DestinationTableName = "#stage", BatchSize = _batchSize })
            {
                bulkCopy.WriteToServer(tempTable);
            }

            // SET clause para UPDATE

            var setClause = string.Join(",", allCols.Where(c => c != _keyCol && c != "SourceKey").Select(c => $"TARGET.{c}=SRC.{c}"));

            // Merge

            var mergeSql = $@"
                MERGE {_table} AS TARGET
                USING #stage AS SRC
                ON TARGET.SourceKey = SRC.SourceKey
                WHEN MATCHED THEN
                    UPDATE SET {setClause}
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT ({insertCols})
                    VALUES ({insertValues});
                    SELECT @@ROWCOUNT;";

            try
            {
                using (var cmd = new SqlCommand(mergeSql, cnx))
                {
                    rowsAffected = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception)
            {
                _logger.LogWarning("[UPSERT] MERGE falló, procesando fila por fila para capturar errores.");

                foreach (DataRow row in tempTable.Rows)
                {
                    try
                    {
                        var singleMerge = $@"
                        MERGE {_table} AS TARGET
                        USING (SELECT {string.Join(",", allCols.Select(c => $"@{c} AS {c}"))}) AS SRC
                        ON TARGET.SourceKey = SRC.SourceKey
                        WHEN MATCHED THEN
                            UPDATE SET {setClause}
                        WHEN NOT MATCHED BY TARGET THEN
                            INSERT ({insertCols})
                            VALUES ({insertValues});";

                        using var cmdRow = new SqlCommand(singleMerge, cnx);

                        foreach (var col in allCols)
                            cmdRow.Parameters.AddWithValue($"@{col}", row[col] ?? DBNull.Value);

                        int rowCount = cmdRow.ExecuteNonQuery();

                        if (rowCount < 1)
                            _logger.LogWarning("[UPSERT] Fila con SourceKey={Key} no afectada como se esperaba", row["SourceKey"]);
                    }
                    catch (Exception exRow)
                    {
                        _loggerDecorator.LogError(row["SourceKey"]?.ToString(), _originDb, _table, "IndividualMerge", exRow.GetType().ToString(), exRow.Message, "", "0", _targetDb);
                    }
                }
            }

            return rowsAffected;
        }

        public int DeleteBatch(SqlConnection cnx, DataRow[] rows)
        {
            int rowsAffected = 0;

            if (rows.Length == 0) return 0;

            using var cmd = new SqlCommand("", cnx);

            var keys = new List<string>();
            foreach (var row in rows)
            {
                keys.Add(row[_keyCol].ToString());
            }

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
            rowsAffected = cmd.ExecuteNonQuery();

            return rowsAffected;
        }
    }
}
