using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidationEngine.Repository
{
    public class SqlConnectionBuilder
    {
        private string _server;
        private string _user;
        private string _password;

        private static readonly Lazy<SqlConnectionBuilder> _instance = new Lazy<SqlConnectionBuilder>(() => new SqlConnectionBuilder());

        public static SqlConnectionBuilder Instance => _instance.Value;

        private SqlConnectionBuilder() 
        { 
        }

        public void Configure(string server, string user, string password)
        {
            _server = server;
            _user = user;
            _password = password;
        }

        public string BuildConnectionString(string database)
        {
            if (!string.IsNullOrWhiteSpace(_user) && !string.IsNullOrWhiteSpace(_password))
            {
                // SQL Authentication
                return $"Server={_server};Database={database};User Id={_user};Password={_password};TrustServerCertificate=True;";
            }
            else
            {
                // Windows Authentication
                return $"Server={_server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;";
            }
        }

        public SqlConnection CreateConnection(string database)
        {
            return new SqlConnection(BuildConnectionString(database));
        }
    }
}
