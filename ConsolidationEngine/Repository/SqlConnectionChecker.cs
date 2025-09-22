using ConsolidationEngine.Config;
using ConsolidationEngine.Logger.Exceptions;
using Microsoft.Data.SqlClient;
using System.Xml.Linq;

namespace ConsolidationEngine.Repository
{
    public class SqlConnectionChecker
    {
        private readonly ConsolidationSettings _settings;
        private readonly ILogger _logger;

        public SqlConnectionChecker(ConsolidationSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public void ValidateConnections()
        {
            _logger.LogInformation("[CONNECTION CHECKER] Iniciando checkeo de conexiones.");

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
            }

            _logger.LogInformation("[CONNECTION CHECKER] {conteo} conexiones OK. {origen} conexion(es) origen, {destino} conexion(es) destino.", checkedDbs.Count, origin, target);
        }


        private void CheckConnection(SqlConnection cnx, string dbName)
        {
            try
            {
                cnx.Open();
                using var cmd = cnx.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.ExecuteScalar();

                _logger.LogInformation("[CONNECTION CHECKER] Conexión exitosa a {Database}", dbName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CONNECTION CHECKER ERROR] Conexión fallida a {Database}", dbName);
                throw new DatabaseConnectionValidatorError($"Error conectando a la base {dbName}", dbName, ex);
            }
            finally
            {
                if (cnx.State == System.Data.ConnectionState.Open)
                    cnx.Close();
            }
        }
    }
}
