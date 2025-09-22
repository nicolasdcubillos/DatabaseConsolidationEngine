using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidationEngine.Repository
{
    public static class SqlRepository
    {
        public static async Task InsertAsync(string tableName, Dictionary<string, object> data, string database, SqlTransaction? transaction = null)
        {
            SqlConnection connection = SqlConnectionBuilder.Instance.CreateConnection(database);
            bool disposeConnection = true;
            await connection.OpenAsync();
            string commandText = string.Empty;

            try
            {
                var columns = string.Join(", ", data.Keys);
                var parameters = string.Join(", ", data.Keys.Select(k => "@" + k));
                commandText = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

                using SqlCommand command = transaction != null ? new SqlCommand(commandText, connection, transaction) : new SqlCommand(commandText, connection);

                foreach (var kvp in data)
                {
                    var paramName = "@" + kvp.Key;
                    var value = kvp.Value ?? DBNull.Value;

                    if (DateTime.TryParse(value.ToString(), out DateTime dt))
                    {
                        command.Parameters.Add(paramName, SqlDbType.DateTime).Value = dt;
                    }
                    else
                    {
                        command.Parameters.AddWithValue(paramName, value);
                    }
                }

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                var paramInfo = string.Join(", ", data.Select(kvp => $"@{kvp.Key} = {kvp.Value ?? "NULL"}"));
                var errorMsg = $"[InsertAsync] Error al ejecutar1\nSQL: {commandText}\nParams: {paramInfo}\nTabla: {tableName}\nError: {ex.Message}";
                throw new Exception(errorMsg, ex);
            }
            finally
            {
                if (disposeConnection && connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                    connection?.Dispose();
                }
            }
        }

        public static async Task<List<Dictionary<string, object>>> SelectAsync(string tableName, string database, string? whereClause = null, SqlTransaction? transaction = null)
        {
            SqlConnection connection = SqlConnectionBuilder.Instance.CreateConnection(database);
            bool disposeConnection = true;
            await connection.OpenAsync();

            string query = string.Empty;
            try
            {
                query = $"SELECT * FROM {tableName}";
                if (!string.IsNullOrWhiteSpace(whereClause))
                    query += $" WHERE {whereClause}";

                using SqlCommand command = transaction != null ? new SqlCommand(query, connection, transaction) : new SqlCommand(query, connection);

                using var reader = await command.ExecuteReaderAsync();

                var results = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                    }

                    results.Add(row);
                }

                return results;
            }
            catch (Exception ex)
            {
                var errorMsg = $"[SelectAsync] Error al ejecutar:\nSQL: {query}\nTabla: {tableName}\nError: {ex.Message}";
                throw new Exception(errorMsg, ex);
            }
            finally
            {
                if (disposeConnection && connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                    connection?.Dispose();
                }
            }
        }

        public static async Task UpdateAsync(string tableName, Dictionary<string, object> data, string whereClause, string database, SqlTransaction? transaction = null)
        {
            SqlConnection connection = SqlConnectionBuilder.Instance.CreateConnection(database);
            bool disposeConnection = true;
            await connection.OpenAsync();

            string commandText = string.Empty;
            try
            {
                var setClause = string.Join(", ", data.Keys.Select(k => $"{k} = @{k}"));
                commandText = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";

                using SqlCommand command = transaction != null ? new SqlCommand(commandText, connection, transaction) : new SqlCommand(commandText, connection);

                foreach (var kvp in data)
                {
                    var paramName = "@" + kvp.Key;
                    var value = kvp.Value ?? DBNull.Value;

                    if (DateTime.TryParse(value.ToString(), out DateTime dt) && dt.Year >= 2000 && dt.Year <= 2100)
                    {
                        command.Parameters.Add(paramName, SqlDbType.DateTime).Value = dt;
                    }
                    else
                    {
                        command.Parameters.AddWithValue(paramName, value);
                    }
                }


                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                var paramInfo = string.Join(", ", data.Select(kvp => $"@{kvp.Key} = {kvp.Value ?? "NULL"}"));
                var errorMsg = $"[UpdateAsync] Error al ejecutar:\nSQL: {commandText}\nParams: {paramInfo}\nTabla: {tableName}\nError: {ex.Message}";
                throw new Exception(errorMsg, ex);
            }
            finally
            {
                if (disposeConnection && connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                    connection?.Dispose();
                }
            }
        }
    }
}
