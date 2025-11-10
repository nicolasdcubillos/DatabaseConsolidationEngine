using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.ComponentModel;
using System.Data;

namespace ConsolidationDashboard
{
    public partial class ConsolidationDashboard : Form
    {
        private System.Windows.Forms.Timer refreshTimer;

        public ConsolidationDashboard()
        {
            InitializeComponent();
            // Timer para refrescar cada 5 segundos
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000; // 5 segundos
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            LoadSyncStatus();
            LoadErrorDetails();
            LoadLogs();
            UpdatePieChart();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            LoadSyncStatus();
            LoadErrorDetails();
            LoadLogs();
            ApplyModernGridTheme(GeneralDashboard);
            ApplyModernGridTheme(DetalleErroresGrid);
            ApplyModernGridTheme(logsGrid);
            UpdatePieChart();
        }

        private void ApplyModernGridTheme(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = System.Drawing.Color.White;
            grid.GridColor = System.Drawing.Color.LightGray;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(44, 62, 80);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.BackColor = System.Drawing.Color.White;
            grid.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            grid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            grid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 30;
        }

        private void LoadSyncStatus()
        {
            string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
            var data = new List<dynamic>();
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("EXEC dbo.ConsolidationEngineStatus", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new
                        {
                            SourceDB = reader["SourceDB"].ToString(),
                            LocalVersion = reader["LocalVersion"] == DBNull.Value ? "" : reader["LocalVersion"].ToString(),
                            ConsolidadaVersion = reader["ConsolidadaVersion"] == DBNull.Value ? "" : reader["ConsolidadaVersion"].ToString(),
                            Estado = reader["Estado"].ToString(),
                            Errores = reader["Errores"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Errores"]),
                            ultimoError = reader["ultimoError"] == DBNull.Value ? "" : reader["ultimoError"].ToString()
                        });
                    }
                }
            }
            GeneralDashboard.DataSource = null;
            GeneralDashboard.Rows.Clear();
            GeneralDashboard.Columns.Clear();
            if (data.Count == 0)
            {
                GeneralDashboard.Columns.Add("SourceDB", "Base de datos");
                GeneralDashboard.Columns.Add("LocalVersion", "Version local");
                GeneralDashboard.Columns.Add("ConsolidadaVersion", "Version consolidada");
                GeneralDashboard.Columns.Add("Estado", "Estado");
                GeneralDashboard.Columns.Add("Errores", "Conteo de errores");
                GeneralDashboard.Columns.Add("ultimoError", "Ultimo error");
            }
            else
            {
                GeneralDashboard.DataSource = data;
                GeneralDashboard.Columns["SourceDB"].HeaderText = "Base de datos";
                GeneralDashboard.Columns["LocalVersion"].HeaderText = "Version local";
                GeneralDashboard.Columns["ConsolidadaVersion"].HeaderText = "Version consolidada";
                GeneralDashboard.Columns["Estado"].HeaderText = "Estado";
                GeneralDashboard.Columns["Errores"].HeaderText = "Conteo de errores";
                GeneralDashboard.Columns["ultimoError"].HeaderText = "Ultimo error";
            }
        }

        private void LoadErrorDetails()
        {
            string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
            var data = new List<dynamic>();
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("SELECT * FROM dbo.ConsolidationEngineErrorsView", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new
                        {
                            ID = reader["ID"],
                            SourceDatabase = reader["SourceDatabase"].ToString(),
                            TableName = reader["TableName"].ToString(),
                            ErrorMessage = reader["ErrorDetails"].ToString(),
                            CreatedAt = reader["CreatedAt"] == DBNull.Value ? "" : reader["CreatedAt"].ToString()
                        });
                    }
                }
            }
            DetalleErroresGrid.DataSource = null;
            DetalleErroresGrid.Rows.Clear();
            DetalleErroresGrid.Columns.Clear();
            DetalleErroresGrid.Columns.Add("SourceDatabase", "Base de datos");
            DetalleErroresGrid.Columns.Add("TableName", "Tabla");
            DetalleErroresGrid.Columns.Add("ErrorMessage", "Error");
            DetalleErroresGrid.Columns.Add("CreatedAt", "Fecha");
            DetalleErroresGrid.Columns.Add("ID", "ID");
            DetalleErroresGrid.Columns["ID"].Visible = false;
            foreach (var item in data)
            {
                DetalleErroresGrid.Rows.Add(item.SourceDatabase, item.TableName, item.ErrorMessage, item.CreatedAt, item.ID);
            }
        }

        private void LoadLogs()
        {
            string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
            var data = new List<dynamic>();
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("SELECT * FROM dbo.ConsolidationEngineLogsView", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        data.Add(row);
                    }
                }
            }
            logsGrid.DataSource = null;
            logsGrid.Rows.Clear();
            logsGrid.Columns.Clear();
            if (data.Count > 0)
            {
                var firstRow = (Dictionary<string, object>)data[0];
                foreach (var key in firstRow.Keys)
                {
                    logsGrid.Columns.Add(key, key);
                }
                foreach (Dictionary<string, object> row in data)
                {
                    logsGrid.Rows.Add(row.Values.ToArray());
                }
            }
        }

        private void UpdatePieChart()
        {
            int consolidadas = 0;
            int noConsolidadas = 0;
            string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("EXEC dbo.ConsolidationEngineStatus", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var estado = reader["Estado"].ToString().ToLower();
                        if (estado.Contains("sincronizada") || estado.Contains("consolidada"))
                            consolidadas++;
                        else
                            noConsolidadas++;
                    }
                }
            }
            pieChart.Series["ConsolidationStatus"].Points.Clear();
            pieChart.Series["ConsolidationStatus"].Points.AddXY("Consolidadas", consolidadas);
            pieChart.Series["ConsolidationStatus"].Points.AddXY("No consolidadas", noConsolidadas);
            pieChart.Series["ConsolidationStatus"].IsValueShownAsLabel = true;
            pieChart.Series["ConsolidationStatus"].LabelForeColor = System.Drawing.Color.White;
            pieChart.Series["ConsolidationStatus"].Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.BrightPastel;
        }

        private void DetalleErroresGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Previously toggled checkbox selection. No longer needed since we retry all errors with a button.
        }

        private void RetryAllErrors()
        {
            string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.ConsolidationEngineRetryAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            LoadErrorDetails();
        }

        private void btnRetryErrors_Click(object sender, EventArgs e)
        {
            RetryAllErrors();
        }

        private void btnActualizar_Click(object sender, EventArgs e)
        {
            // Disable button to prevent re-entrancy
            btnActualizar.Enabled = false;
            // Stop automatic refresh while performing manual update
            refreshTimer.Stop();
            try
            {
                LoadSyncStatus();
                LoadErrorDetails();
                LoadLogs();
                UpdatePieChart();
            }
            finally
            {
                refreshTimer.Start();
                btnActualizar.Enabled = true;
            }
        }

        private void GeneralDashboard_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dashboardTitle_Click(object sender, EventArgs e)
        {

        }
    }
}
