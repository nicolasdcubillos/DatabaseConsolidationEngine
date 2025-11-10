using ConsolidationEngine.Config;
using ConsolidationEngine.Logger.Exceptions;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace ConsolidationEngine.Repository
{
    public class SqlSchemaValidator
    {
        private readonly ConsolidationSettings _settings;
        private readonly ILogger _logger;

        public SqlSchemaValidator(ConsolidationSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public void Validate()
        {
            _logger.LogInformation("[SCHEMA VALIDATOR] Iniciando validación de conexiones y esquemas.");

            var checkedDbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int origin = 0;
            int target = 0;

            foreach (var dbPair in _settings.Databases)
            {
                if (checkedDbs.Add(dbPair.Origin))
                {
                    CheckConnection(SqlConnectionBuilder.Instance.CreateConnection(dbPair.Origin), dbPair.Origin);
                    origin++;
                }

                if (checkedDbs.Add(dbPair.Target))
                {
                    CheckConnection(SqlConnectionBuilder.Instance.CreateConnection(dbPair.Target), dbPair.Target);
                    target++;
                }

                ValidateSchema(dbPair);
            }

            _logger.LogInformation("[SCHEMA VALIDATOR] Validación completada: {origen} DBs origen, {destino} DBs destino.", origin, target);
        }

        private void CheckConnection(SqlConnection cnx, string dbName)
        {
            try
            {
                cnx.Open();
                using var cmd = cnx.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.ExecuteScalar();
                _logger.LogInformation("[SCHEMA VALIDATOR] Conexión exitosa a {Database}", dbName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCHEMA VALIDATOR ERROR] Conexión fallida a {Database}", dbName);
                throw new DatabaseConnectionValidatorError($"Error conectando a la base {dbName}", dbName, ex);
            }
            finally
            {
                if (cnx.State == ConnectionState.Open)
                    cnx.Close();
            }
        }

        private void ValidateSchema(DatabasePairConfig dbPair)
        {
            using var originCnx = SqlConnectionBuilder.Instance.CreateConnection(dbPair.Origin);
            using var targetCnx = SqlConnectionBuilder.Instance.CreateConnection(dbPair.Target);
            originCnx.Open();
            targetCnx.Open();

            foreach (var table in _settings.Tables)
            {
                var originColumns = GetColumns(originCnx, table.Name);
                var targetColumns = GetColumns(targetCnx, table.Name);

                var missingColumns = originColumns
                    .Where(c => !targetColumns.ContainsKey(c.Key))
                    .ToList();

                if (missingColumns.Any())
                {
                    _logger.LogWarning("[SCHEMA VALIDATOR] Faltan {count} columnas en {table} en {target}.", missingColumns.Count, table.Name, dbPair.Target);
                    foreach (var missing in missingColumns)
                    {
                        AddColumn(targetCnx, table.Name, missing.Value);
                        _logger.LogInformation("[SCHEMA VALIDATOR] Columna agregada: {table}.{col} ({type})", table.Name, missing.Value.ColumnName, missing.Value.DataType);
                    }
                }
                else
                {
                    _logger.LogInformation("[SCHEMA VALIDATOR] {table}: schema OK en {target}.", table.Name, dbPair.Target);
                }
            }

            _logger.LogInformation("[SCHEMA VALIDATOR] Conexión OK y schema OK entre {origin} -> {target}", dbPair.Origin, dbPair.Target);
        }

        private Dictionary<string, ColumnDefinition> GetColumns(SqlConnection cnx, string tableName)
        {
            var columns = new Dictionary<string, ColumnDefinition>(StringComparer.OrdinalIgnoreCase);

            // Asegura que el nombre no incluya "dbo."
            var rawTableName = tableName.Contains(".")
                ? tableName.Split('.').Last()
                : tableName;

            var query = @"
                SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo'
                  AND TABLE_NAME = @table";

            using var cmd = new SqlCommand(query, cnx);
            cmd.Parameters.AddWithValue("@table", rawTableName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var col = new ColumnDefinition
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? null : reader.GetInt32(2)
                };
                columns[col.ColumnName] = col;
            }

            return columns;
        }


        private void AddColumn(SqlConnection cnx, string tableName, ColumnDefinition column)
        {
            string typeClause = column.DataType.ToUpper() switch
            {
                "VARCHAR" or "NVARCHAR" => $"{column.DataType}({(column.MaxLength == -1 ? "MAX" : column.MaxLength)})",
                _ => column.DataType
            };

            string alterSql = $"ALTER TABLE {tableName} ADD [{column.ColumnName}] {typeClause} NULL";

            using var cmd = new SqlCommand(alterSql, cnx);
            cmd.ExecuteNonQuery();
        }

        private class ColumnDefinition
        {
            public string ColumnName { get; set; }
            public string DataType { get; set; }
            public int? MaxLength { get; set; }
        }
    }
}
