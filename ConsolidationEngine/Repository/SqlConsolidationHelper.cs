using ConsolidationEngine.Logger;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly DualLogger _dualLogger;
        private readonly bool _skipPrimaryKey;
        private readonly int _upsertBatchWithFallbackTimeoutSeconds;
        private readonly string _rowFilter;

        public SqlConsolidationHelper(
            string server,
            string originDb,
            string targetDb,
            string table,
            string keyCol,
            int batchSize,
            bool skipPrimaryKey,
            int upsertBatchWithFallbackTimeoutSeconds,
            ILogger logger,
            string rowFilter = null)
        {
            _server = server;
            _originDb = originDb;
            _targetDb = targetDb;
            _table = table;
            _keyCol = keyCol;
            _batchSize = batchSize;
            _logger = logger;
            _dualLogger = new DualLogger(logger);
            _skipPrimaryKey = skipPrimaryKey;
            _upsertBatchWithFallbackTimeoutSeconds = upsertBatchWithFallbackTimeoutSeconds;
            _rowFilter = rowFilter;
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

        public FetchResult FetchChanges(SqlConnection cnx, long fromVersion, long toVersion)
        {
            // Si hay un RowFilter definido, se agrega como condición adicional en el WHERE.
            // Se usa "s" como alias de la tabla origen; el filtro debe respetar ese alias.
            // Para eliminaciones (D), s.* es NULL por el LEFT JOIN, por eso se deja pasar
            // siempre con la condición OR 'D', evitando que las DELETEs queden bloqueadas.
            var rowFilterClause = !string.IsNullOrWhiteSpace(_rowFilter)
                ? $"AND (ct.SYS_CHANGE_OPERATION = 'D' OR ({_rowFilter}))"
                : string.Empty;

            var sqlFiltered = $@"
                DECLARE @from BIGINT = @fromVersion;
                DECLARE @min BIGINT = CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('{_table}'));
                IF @from < @min
                    RAISERROR('WATERMARK_TOO_OLD', 16, 1);

                SELECT ct.SYS_CHANGE_VERSION, ct.SYS_CHANGE_OPERATION, ct.{_keyCol} AS CT_KEYCOL, s.*
                FROM CHANGETABLE(CHANGES {_table}, @from) AS ct
                LEFT JOIN {_table} AS s ON s.{_keyCol} = ct.{_keyCol}
                WHERE ct.SYS_CHANGE_VERSION <= {toVersion}
                {rowFilterClause}
                ORDER BY ct.SYS_CHANGE_VERSION, ct.{_keyCol};";

            using var cmd = new SqlCommand(sqlFiltered, cnx);
            cmd.Parameters.AddWithValue("@fromVersion", fromVersion);

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);

            // Sin RowFilter: el total coincide exactamente con lo retornado, sin viaje extra a la BD.
            if (string.IsNullOrWhiteSpace(_rowFilter))
                return new FetchResult { ChangedRows = dt, TotalChangesInRange = dt.Rows.Count };

            // Con RowFilter activo: consulta liviana de COUNT para saber cuántos cambios hubo
            // en el rango antes del filtro. Solo se usa para logging, no impacta el procesamiento.
            var sqlCount = $@"
                SELECT COUNT(*)
                FROM CHANGETABLE(CHANGES {_table}, @fromVersion) AS ct
                WHERE ct.SYS_CHANGE_VERSION <= {toVersion};";

            using var countCmd = new SqlCommand(sqlCount, cnx);
            countCmd.Parameters.AddWithValue("@fromVersion", fromVersion);
            int totalInRange = (int)countCmd.ExecuteScalar();

            return new FetchResult { ChangedRows = dt, TotalChangesInRange = totalInRange };
        }

        public int UpsertBatchWithFallback(string targetDb, DataRow[] rows)
        {
            using SqlConnection cnx = SqlConnectionBuilder.Instance.CreateConnection(targetDb);
            cnx.Open();
            string mergeSqlForLog = null;

            using (var dateCmd = new SqlCommand("SET DATEFORMAT ymd;", cnx))
                dateCmd.ExecuteNonQuery();

            int rowsAffected = 0;

            if (rows.Length == 0) return 0;

            // Columnas de la tabla sin tracking

            var allCols = rows[0].Table.Columns
                .Cast<DataColumn>()
                .Where(c => c.ColumnName != "SYS_CHANGE_VERSION" && c.ColumnName != "SYS_CHANGE_OPERATION" && c.ColumnName != "CT_KEYCOL")
                .Select(c => c.ColumnName)
                .ToList();

            // Agregar columna SourceKey

            if (!allCols.Contains("SourceKey"))
                allCols.Add("SourceKey");

            // Cuando se debe skipear la PK, la excluimos de los inserts y updates
            var colsExcludingPkForInsert = _skipPrimaryKey
                ? allCols.Where(c => !string.Equals(c, _keyCol, StringComparison.OrdinalIgnoreCase)).ToList()
                : allCols.ToList();

            var insertCols = string.Join(",", colsExcludingPkForInsert);
            var insertValues = string.Join(",", colsExcludingPkForInsert.Select(c => $"SRC.{c}"));

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

            // SET clause para UPDATE (si skipeamos PK, no está en la lista de columnas a setear)

            var setClauseCols = _skipPrimaryKey
                ? allCols.Where(c => !string.Equals(c, _keyCol, StringComparison.OrdinalIgnoreCase) && c != "SourceKey")
                : allCols.Where(c => c != "SourceKey");

            var setClause = string.Join(",", setClauseCols.Select(c => $"TARGET.{c}=SRC.{c}"));

            // Merge

            var joinCondition = _skipPrimaryKey
                ? "TARGET.SourceKey = SRC.SourceKey"
                : $"TARGET.{_keyCol} = SRC.{_keyCol}";

            string mergeSql = $@"
                MERGE {_table} AS TARGET
                USING #stage AS SRC
                ON {joinCondition}
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
                    mergeSqlForLog = GetLiteralSql(cmd);
                    cmd.CommandTimeout = _upsertBatchWithFallbackTimeoutSeconds;
                    rowsAffected = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                string singleMerge = null;

                _dualLogger.LogError(
                            "",
                            _originDb,
                            _table,
                            "UpsertBatchWithFallback",
                            ex.GetType().ToString(),
                            ex.Message,
                            mergeSqlForLog ?? "",
                            "0",
                            _targetDb
                        );

                foreach (DataRow row in tempTable.Rows)
                {
                    SqlConnection retryConnection = null;

                    try
                    {
                        retryConnection = SqlConnectionBuilder.Instance.CreateConnection(targetDb);
                        retryConnection.Open();

                        ExecuteSingleMerge(
                            cnx: retryConnection,
                            tableName: _table,
                            allCols: allCols,
                            setClause: setClause,
                            insertCols: insertCols,
                            insertValues: insertValues,
                            row: row,
                            onWatermarkUpdated: version => SetWatermark(cnx, version),
                            logger: _logger,
                            skipPrimaryKey: _skipPrimaryKey,
                            keyCol: _keyCol,
                            out singleMerge
                        );

                        retryConnection.Close();
                    }
                    catch (Exception exRow)
                    {
                        string fullMessage = exRow.Message;
                        if (exRow.InnerException != null)
                        {
                            fullMessage += $" | Inner: {exRow.InnerException.Message}";
                        }

                        _dualLogger.LogError(
                            row["SourceKey"]?.ToString(),
                            _originDb,
                            _table,
                            "IndividualMerge",
                            exRow.GetType().ToString(),
                            fullMessage,
                            singleMerge ?? "",
                            "0",
                            _targetDb
                        );
                    }
                    finally
                    {
                        retryConnection?.Close();
                    }
                }
            }

            cnx.Close();

            return rowsAffected;
        }

        public static void ExecuteSingleUpsert(
            SqlConnection cnx,
            string tableName,
            string keyCol,
            IReadOnlyList<string> allCols,
            DataRow row,
            ILogger logger)
        {
            using var tran = cnx.BeginTransaction();

            try
            {
                if (!allCols.Contains("SourceKey"))
                    throw new ArgumentException("La lista de columnas debe incluir 'SourceKey'.");

                string setClause = string.Join(", ", allCols
                    .Where(c => c != keyCol && c != "SourceKey")
                    .Select(c => $"T.{c} = @{c}"));

                string insertCols = string.Join(", ", allCols);
                string insertValues = string.Join(", ", allCols.Select(c => $"@{c}"));

                var updateSql = $@"
                    UPDATE T
                    SET {setClause}
                    FROM {tableName} AS T
                    WHERE T.SourceKey = @SourceKey;";

                using var cmdUpdate = new SqlCommand(updateSql, cnx, tran);
                foreach (var col in allCols)
                    cmdUpdate.Parameters.AddWithValue($"@{col}", row[col] ?? DBNull.Value);

                int updated = cmdUpdate.ExecuteNonQuery();

                if (updated == 0)
                {
                    var insertSql = $@"
                INSERT INTO {tableName} ({insertCols})
                VALUES ({insertValues});";

                    using var cmdInsert = new SqlCommand(insertSql, cnx, tran);
                    foreach (var col in allCols)
                        cmdInsert.Parameters.AddWithValue($"@{col}", row[col] ?? DBNull.Value);

                    try
                    {
                        cmdInsert.ExecuteNonQuery();
                    }
                    catch (SqlException ex) when (ex.Number == 2627) // PK violation
                    {
                        // Otra transacción insertó al mismo tiempo → hacemos UPDATE
                        cmdUpdate.ExecuteNonQuery();
                    }
                }

                tran.Commit();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                logger.LogError(ex, "Error en upsert de SourceKey={Key}", row["SourceKey"]);
                throw;
            }
        }

        public static void ExecuteSingleMerge(
           SqlConnection cnx,
           string tableName,
           IEnumerable<string> allCols,
           string setClause,
           string insertCols,
           string insertValues,
           DataRow row,
           Action<long>? onWatermarkUpdated,
           ILogger logger,
           bool skipPrimaryKey,
           string keyCol,
           out string sqlCommandToRetry)
        {
            using (var dateCmd = new SqlCommand("SET DATEFORMAT ymd;", cnx))
                dateCmd.ExecuteNonQuery();

            var joinCondition = skipPrimaryKey
                ? "TARGET.SourceKey = SRC.SourceKey"
                : $"TARGET.{keyCol} = SRC.{keyCol}";


            string singleMerge = $@"
            MERGE {tableName} AS TARGET
            USING (SELECT {string.Join(",", allCols.Select(c => $"@{c} AS {c}"))}) AS SRC
            ON {joinCondition}
            WHEN MATCHED THEN
                UPDATE SET {setClause}
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ({insertCols})
                VALUES ({insertValues});";

            try
            {
                using var cmdRow = new SqlCommand(singleMerge, cnx);

                foreach (var col in allCols)
                    cmdRow.Parameters.AddWithValue($"@{col}", row[col] ?? DBNull.Value);

                sqlCommandToRetry = GetLiteralSql(cmdRow);

                int rowCount = cmdRow.ExecuteNonQuery();

                if (rowCount < 1)
                {
                    logger.LogWarning("[UPSERT] Fila con SourceKey={Key} no fue afectada como se esperaba", row["SourceKey"]);
                }
                else
                {
                    if (row.Table.Columns.Contains("SYS_CHANGE_VERSION") && row["SYS_CHANGE_VERSION"] != DBNull.Value)
                    {
                        long version = Convert.ToInt64(row["SYS_CHANGE_VERSION"]);
                        //onWatermarkUpdated?.Invoke(version);
                    }
                }
            }
            catch (Exception ex)
            {
                //logger.LogError(ex, "[UPSERT] Error ejecutando MERGE individual para SourceKey={Key}", row["SourceKey"]);
                throw;
            }
        }

        internal static string GetLiteralSql(SqlCommand cmd)
        {
            string sql = cmd.CommandText;

            foreach (SqlParameter p in cmd.Parameters)
            {
                string literal;

                if (p.Value == DBNull.Value || p.Value == null)
                {
                    literal = "NULL";
                }
                else if (p.Value is string s)
                {
                    literal = $"'{s.Replace("'", "''")}'";
                }
                else if (p.Value is DateTime dt)
                {
                    // formato compatible con SQL Server
                    literal = $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
                }
                else if (p.Value is bool b)
                {
                    literal = b ? "1" : "0";
                }
                else if (p.Value is byte[] bytes)
                {
                    // convertir binarios a formato hexadecimal 0x...
                    literal = "0x" + BitConverter.ToString(bytes).Replace("-", "");
                }
                else
                {
                    // números, decimales, etc.
                    literal = Convert.ToString(p.Value, System.Globalization.CultureInfo.InvariantCulture);
                }

                // Reemplazar solo coincidencias exactas del nombre del parámetro (con borde de palabra)
                sql = Regex.Replace(
                    sql,
                    $@"(?<!\w){Regex.Escape(p.ParameterName)}(?!\w)",
                    literal);
            }

            return sql;
        }

        private static string GetSqlType(Type type)
        {
            if (type == typeof(long)) return "BIGINT";
            if (type == typeof(int)) return "INT";
            if (type == typeof(Guid)) return "UNIQUEIDENTIFIER";
            if (type == typeof(string)) return "NVARCHAR(200)";

            throw new NotSupportedException(
                $"Primary key type '{type.Name}' is not supported");
        }

        public int DeleteBatch(SqlConnection cnx, DataRow[] rows)
        {
            if (rows == null || rows.Length == 0)
                return 0;

            // 1. Obtener el tipo real de la PK desde el DataTable (ct.{keyCol})
            var pkColumn = rows[0].Table.Columns["CT_KEYCOL"];
            var pkType = pkColumn.DataType;

            // 2. Crear DataTable con las PKs
            var dtKeys = new DataTable();
            dtKeys.Columns.Add("k", pkType);

            foreach (var row in rows)
            {
                if (row["CT_KEYCOL"] == DBNull.Value)
                    throw new InvalidOperationException("DELETE change without PK");

                dtKeys.Rows.Add(row["CT_KEYCOL"]);
            }

            using var cmd = new SqlCommand();
            cmd.Connection = cnx;
            cmd.CommandType = CommandType.Text;

            // 3. Crear tabla temporal con el TIPO CORRECTO
            string sqlPkType = GetSqlType(pkType);

            cmd.CommandText = $@"
                IF OBJECT_ID('tempdb..#del') IS NOT NULL DROP TABLE #del;
                CREATE TABLE #del (
                    k {sqlPkType} NOT NULL
                );";
            cmd.ExecuteNonQuery();

            // 4. Bulk insert de las PKs
            using (var bulkCopy = new SqlBulkCopy(cnx))
            {
                bulkCopy.DestinationTableName = "#del";
                bulkCopy.BatchSize = _batchSize;
                bulkCopy.WriteToServer(dtKeys);
            }

            // 5. DELETE según estrategia
            cmd.Parameters.Clear();

            if (_skipPrimaryKey)
            {
                cmd.CommandText = $@"
                DELETE T
                FROM {_table} AS T
                INNER JOIN #del AS D
                    ON T.SourceKey = CONCAT(@OriginDb, '_', D.k);";

                cmd.Parameters.Add("@OriginDb", SqlDbType.NVarChar, 50).Value = _originDb;
            }
            else
            {
                cmd.CommandText = $@"
                    DELETE T
                    FROM {_table} AS T
                    INNER JOIN #del AS D
                        ON T.{_keyCol} = D.k;";
            }

            int rowsAffected = cmd.ExecuteNonQuery();

            return rowsAffected;
        }
    }
}
