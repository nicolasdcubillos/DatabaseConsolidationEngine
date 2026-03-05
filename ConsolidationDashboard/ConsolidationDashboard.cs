using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace ConsolidationDashboard
{
    public partial class ConsolidationDashboard : Form
    {
        private System.Windows.Forms.Timer refreshTimer;
        private bool isLoading = false;
        private bool isClosing = false;
        private Label statusLabel;

        // Paleta de colores por estado
        private static readonly Color ColorOk = Color.FromArgb(39, 174, 96);
        private static readonly Color ColorError = Color.FromArgb(192, 57, 43);
        private static readonly Color ColorWarning = Color.FromArgb(230, 126, 34);
        private static readonly Color ColorUnknown = Color.FromArgb(127, 140, 141);
        private static readonly Color CardBg = Color.FromArgb(250, 251, 252);
        private static readonly Color CardBorder = Color.FromArgb(220, 220, 220);

        // Colores de sección
        private static readonly Color SectionHeaderBg = Color.FromArgb(44, 62, 80);
        private static readonly Color SectionHeaderText = Color.White;
        private static readonly Color SectionBg = Color.White;
        private static readonly Color SectionBorder = Color.FromArgb(210, 215, 220);

        public ConsolidationDashboard()
        {
            InitializeComponent();
            InitializeHeaderRight();   // crea el panel derecho unificado (botón + status)
            StyleRefreshButton();
            StyleSectionPanels();   // estiliza las secciones de errores y logs

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000;
            refreshTimer.Tick += RefreshTimer_Tick;
        }

        // ── Panel derecho del header: botón + pill de estado ─────────────────
        private void InitializeHeaderRight()
        {
            // Contenedor visual con borde suave, alineado a la derecha del bloque de KPIs
            // X = 1185  (margen derecho de 15px desde el edge del mainPanel a 1600px)
            // Y = 85    (mismo alineamiento vertical que los KPIs en Y=88)
            // W = 390, H = 76
            var rightPanel = new Panel
            {
                Location  = new Point(1185, 83),
                Size      = new Size(390, 76),
                BackColor = Color.Transparent
            };

            rightPanel.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(210, 215, 220), 1))
                using (var path = GetRoundedRect(new Rectangle(0, 0, rightPanel.Width - 1, rightPanel.Height - 1), 12))
                    pe.Graphics.DrawPath(pen, path);
            };

            // ── Botón actualizar: fila superior, ocupa todo el ancho ─────────
            btnActualizar.Parent   = rightPanel;
            btnActualizar.Location = new Point(0, 0);
            btnActualizar.Size     = new Size(390, 38);

            // ── Pill de estado: fila inferior ────────────────────────────────
            statusLabel = new Label
            {
                AutoSize  = false,
                Size      = new Size(390, 36),
                Location  = new Point(0, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                BackColor = Color.Transparent,
                Text      = "⏱  Listo"
            };

            statusLabel.Paint += (s, pe) =>
            {
                var lbl = (Label)s;
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var brush = new SolidBrush(Color.FromArgb(240, 243, 246)))
                using (var pen   = new Pen(Color.FromArgb(210, 215, 220), 1))
                using (var path  = GetRoundedRect(new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1), 18))
                {
                    pe.Graphics.FillPath(brush, path);
                    pe.Graphics.DrawPath(pen, path);
                }

                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
                TextRenderer.DrawText(pe.Graphics, lbl.Text, lbl.Font,
                    new Rectangle(0, 0, lbl.Width, lbl.Height), lbl.ForeColor, flags);
            };

            rightPanel.Controls.Add(statusLabel);
            mainPanel.Controls.Add(rightPanel);
            rightPanel.BringToFront();
        }

        private void StyleRefreshButton()
        {
            // Bordes redondeados + hover via Paint y eventos de mouse
            btnActualizar.Paint += (s, e) =>
            {
                var btn = (Button)s;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var radius = 10;
                var rect   = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                var color  = btn.BackColor;

                using (var brush = new SolidBrush(color))
                using (var path  = GetRoundedRect(rect, radius))
                {
                    g.FillPath(brush, path);
                }

                // Texto e ícono
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
                TextRenderer.DrawText(g, btn.Text, btn.Font, rect, btn.ForeColor, flags);
            };

            btnActualizar.MouseEnter += (s, e) =>
            {
                btnActualizar.BackColor = Color.FromArgb(31, 97, 141); // hover más oscuro
                btnActualizar.Invalidate();
            };
            btnActualizar.MouseLeave += (s, e) =>
            {
                btnActualizar.BackColor = Color.FromArgb(41, 128, 185);
                btnActualizar.Invalidate();
            };
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void InitializeStatusLabel() { } // mantenido vacío — lógica movida a InitializeHeaderRight

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isClosing = true;
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || isClosing || isLoading)
                return;
            RefreshAllData();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            try
            {
                if (!ValidateConfiguration())
                {
                    MessageBox.Show("La cadena de conexión no está configurada correctamente en App.config",
                        "Error de configuración", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!TestDatabaseConnection())
                {
                    MessageBox.Show("No se puede conectar a la base de datos. Verifique la configuración y que el servidor SQL esté disponible.",
                        "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Evitar que grids y botones internos roben el foco (causando scroll automático)
                DetalleErroresGrid.TabStop = false;
                logsGrid.TabStop          = false;
                btnRetryErrors.TabStop    = false;
                btnActualizar.TabStop     = false;

                ApplyModernGridTheme(DetalleErroresGrid);
                ApplyModernGridTheme(logsGrid);

                RefreshAllData();
                refreshTimer.Start();
            }
            catch (Exception ex)
            {
                LogError("Form1_Load", ex);
                ShowErrorMessage("Error al inicializar el dashboard", ex);
            }
            finally
            {
                // BeginInvoke garantiza que el reset ocurre DESPUÉS de que WinForms
                // haya terminado el primer layout/paint completo del form
                BeginInvoke(new Action(() =>
                {
                    mainPanel.Focus();
                    scrollContainer.AutoScrollPosition = new Point(0, 0);
                }));
            }
        }

        private bool ValidateConfiguration()
        {
            try
            {
                var connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"];
                return connStr != null && !string.IsNullOrWhiteSpace(connStr.ConnectionString);
            }
            catch (Exception ex) { LogError("ValidateConfiguration", ex); return false; }
        }

        private bool TestDatabaseConnection()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex) { LogError("TestDatabaseConnection", ex); return false; }
        }

        private void ApplyModernGridTheme(DataGridView grid)
        {
            if (grid == null) return;
            try
            {
                grid.EnableHeadersVisualStyles = false;
                grid.BorderStyle = BorderStyle.None;
                grid.BackgroundColor = Color.White;
                grid.GridColor = Color.LightGray;
                grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                grid.DefaultCellStyle.BackColor = Color.White;
                grid.DefaultCellStyle.ForeColor = Color.FromArgb(44, 62, 80);
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.RowHeadersVisible = false;
                grid.RowTemplate.Height = 28;
                grid.ReadOnly = true;
                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
            }
            catch (Exception ex) { LogError("ApplyModernGridTheme", ex); }
        }

        private void RefreshAllData()
        {
            if (isLoading || isClosing) return;
            try
            {
                isLoading = true;
                SetStatus("Actualizando datos...", Color.FromArgb(52, 152, 219));
                LoadSyncStatus();
                LoadErrorDetails();
                LoadLogs();
                SetStatus($"Última actualización: {DateTime.Now:HH:mm:ss}", Color.FromArgb(39, 174, 96));
            }
            catch (Exception ex)
            {
                LogError("RefreshAllData", ex);
                SetStatus("Error al actualizar datos", Color.FromArgb(192, 57, 43));
            }
            finally
            {
                isLoading = false;
            }
        }

        private void SetStatus(string message, Color color)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(message, color))); return; }
            if (statusLabel == null || isClosing) return;

            // Prefijo de ícono según el tipo de mensaje
            string icon = "⏱";
            if (color == Color.FromArgb(39, 174, 96))        icon = "✔";
            else if (color == Color.FromArgb(192, 57, 43))   icon = "✖";
            else if (color == Color.FromArgb(52, 152, 219))  icon = "↻";

            statusLabel.Text      = $"{icon}  {message}";
            statusLabel.ForeColor = color;
            statusLabel.Invalidate(); // repintar la pill
        }

        // ────────────────────────────────────────────────────────────────────
        //  CARDS
        // ────────────────────────────────────────────────────────────────────

        private void LoadSyncStatus()
        {
            if (isClosing) return;
            var data = new List<DbStatusItem>();

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("dbo.ConsolidationEngineStatus", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(new DbStatusItem
                            {
                                SourceDB = GetSafeString(reader, "SourceDB"),
                                LocalVersion = GetSafeString(reader, "LocalVersion"),
                                ConsolidadaVersion = GetSafeString(reader, "ConsolidadaVersion"),
                                Estado = GetSafeString(reader, "Estado"),
                                Errores = GetSafeInt(reader, "Errores"),
                                UltimoError = GetSafeString(reader, "ultimoError")
                            });
                        }
                    }
                }

                if (cardsPanel.InvokeRequired)
                    cardsPanel.Invoke(new Action(() => BuildDbCards(data)));
                else
                    BuildDbCards(data);
            }
            catch (SqlException ex)
            {
                LogError("LoadSyncStatus - SQL Error", ex);
                SetStatus("Error de BD al cargar estado", ColorError);
            }
            catch (Exception ex)
            {
                LogError("LoadSyncStatus", ex);
                SetStatus("Error al cargar estado de sincronización", ColorError);
            }
        }

        private void BuildDbCards(List<DbStatusItem> data)
        {
            if (isClosing) return;

            var scrollPos = scrollContainer.AutoScrollPosition;

            int total   = data.Count;
            int ok      = data.Count(d => GetSyncState(d) == DbSyncState.Ok);
            int errores = data.Count(d => GetSyncState(d) == DbSyncState.Error);

            kpiTotalValue.Text = total.ToString();
            kpiOkValue.Text    = ok.ToString();
            kpiErrorValue.Text = errores.ToString();

            cardsPanel.SuspendLayout();
            cardsPanel.Controls.Clear();

            foreach (var item in data)
                cardsPanel.Controls.Add(CreateDbCard(item));

            cardsPanel.ResumeLayout(true);
            scrollContainer.AutoScrollPosition = new Point(-scrollPos.X, -scrollPos.Y);
        }

        private Panel CreateDbCard(DbStatusItem item)
        {
            DbSyncState syncState = GetSyncState(item);
            Color stateColor      = GetStateColor(syncState);
            string stateIcon      = GetStateIcon(syncState);

            var card = new Panel
            {
                Size      = new Size(280, 140),
                BackColor = CardBg,
                Margin    = new Padding(8),
                Cursor    = Cursors.Default
            };
            card.Paint += (s, e) => DrawCardBorder(e.Graphics, card.Size, stateColor);

            // ── Barra lateral ──────────────────────────────────────────────
            card.Controls.Add(new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(6, 140),
                BackColor = stateColor
            });

            // ── Ícono ──────────────────────────────────────────────────────
            card.Controls.Add(new Label
            {
                Location  = new Point(14, 10),
                Size      = new Size(28, 28),
                Text      = stateIcon,
                Font      = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = stateColor,
                TextAlign = ContentAlignment.MiddleCenter
            });

            // ── Nombre BD ──────────────────────────────────────────────────
            card.Controls.Add(new Label
            {
                Location     = new Point(46, 10),
                Size         = new Size(228, 26),
                Text         = item.SourceDB,
                Font         = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor    = Color.FromArgb(44, 62, 80),
                TextAlign    = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            });

            // ── Estado (texto) ─────────────────────────────────────────────
            card.Controls.Add(new Label
            {
                Location     = new Point(46, 36),
                Size         = new Size(228, 20),
                Text         = item.Estado,
                Font         = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor    = stateColor,
                TextAlign    = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            });

            // ── Separador ─────────────────────────────────────────────────
            card.Controls.Add(new Panel
            {
                Location  = new Point(14, 60),
                Size      = new Size(252, 1),
                BackColor = CardBorder
            });

            // ── Versión local ──────────────────────────────────────────────
            card.Controls.Add(new Label { Location = new Point(14, 68), Size = new Size(80, 16),  Text = "Local",       Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(150, 150, 150), TextAlign = ContentAlignment.MiddleLeft });
            card.Controls.Add(new Label { Location = new Point(14, 83), Size = new Size(120, 18), Text = string.IsNullOrEmpty(item.LocalVersion) ? "—" : item.LocalVersion, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true });

            // ── Versión consolidada ────────────────────────────────────────
            card.Controls.Add(new Label { Location = new Point(148, 68), Size = new Size(120, 16), Text = "Consolidada", Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(150, 150, 150), TextAlign = ContentAlignment.MiddleLeft });
            card.Controls.Add(new Label { Location = new Point(148, 83), Size = new Size(120, 18), Text = string.IsNullOrEmpty(item.ConsolidadaVersion) ? "—" : item.ConsolidadaVersion, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true });

            // ── Badge inferior ─────────────────────────────────────────────
            if (item.Errores > 0)
            {
                card.Controls.Add(new Label
                {
                    Location     = new Point(14, 108),
                    Size         = new Size(252, 22),
                    Text         = $"✖  {item.Errores} error{(item.Errores != 1 ? "es" : "")}",
                    Font         = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor    = stateColor,
                    TextAlign    = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true
                });
            }
            else if (syncState == DbSyncState.Syncing)
            {
                card.Controls.Add(new Label
                {
                    Location  = new Point(14, 108),
                    Size      = new Size(252, 22),
                    Text      = "⟳  Sincronizando...",
                    Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = stateColor,
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }
            else
            {
                card.Controls.Add(new Label
                {
                    Location  = new Point(14, 108),
                    Size      = new Size(252, 22),
                    Text      = "✔  Sin errores",
                    Font      = new Font("Segoe UI", 8.5F),
                    ForeColor = Color.FromArgb(150, 150, 150),
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }

            // ── Tooltip último error ───────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(item.UltimoError))
            {
                var tt = new ToolTip { InitialDelay = 300 };
                tt.SetToolTip(card, $"Último error: {item.UltimoError}");
                foreach (Control c in card.Controls)
                    tt.SetToolTip(c, $"Último error: {item.UltimoError}");
            }

            return card;
        }

        private static void DrawCardBorder(Graphics g, Size size, Color accentColor)
        {
            using (var pen = new Pen(CardBorder, 1))
                g.DrawRectangle(pen, 0, 0, size.Width - 1, size.Height - 1);
        }

        // ── Calcula el estado semántico combinando Estado, versiones y errores ─
        private static DbSyncState GetSyncState(DbStatusItem item)
        {
            // Rojo: cualquier error, sin importar si está sincronizado
            if (item.Errores > 0) return DbSyncState.Error;

            bool isSynced = IsOkState(item.Estado);

            // Naranja: sincronizado según estado PERO versiones distintas (mismatch),
            // o el estado contiene palabras de proceso en curso
            if (isSynced)
            {
                bool versionMismatch = !string.IsNullOrEmpty(item.LocalVersion)
                    && !string.IsNullOrEmpty(item.ConsolidadaVersion)
                    && item.LocalVersion != item.ConsolidadaVersion;
                if (versionMismatch) return DbSyncState.Syncing;
                return DbSyncState.Ok;
            }

            var lower = item.Estado?.ToLowerInvariant() ?? "";
            if (lower.Contains("sincronizando") || lower.Contains("procesando")
                || lower.Contains("pendiente")  || lower.Contains("espera")
                || lower.Contains("progress")   || lower.Contains("running"))
                return DbSyncState.Syncing;

            if (lower.Contains("error") || lower.Contains("fallo") || lower.Contains("fail"))
                return DbSyncState.Error;

            return DbSyncState.Syncing; // cualquier otro estado no-ok sin errores → en proceso
        }

        private static Color GetStateColor(DbSyncState state)
        {
            switch (state)
            {
                case DbSyncState.Ok:     return ColorOk;
                case DbSyncState.Syncing: return ColorWarning;
                default:                 return ColorError;
            }
        }

        // Sobrecarga de compatibilidad (usada por código heredado si quedara alguna llamada)
        private static Color GetStateColor(string estado, int errores)
        {
            var tmp = new DbStatusItem { Estado = estado, Errores = errores };
            return GetStateColor(GetSyncState(tmp));
        }

        private static string GetStateIcon(DbSyncState state)
        {
            switch (state)
            {
                case DbSyncState.Ok:      return "✔";
                case DbSyncState.Syncing: return "⟳";
                default:                  return "✖";
            }
        }

        private static bool IsOkState(string estado)
        {
            if (string.IsNullOrEmpty(estado)) return false;
            var lower = estado.ToLowerInvariant();
            return lower.Contains("sincronizada") || lower.Contains("consolidada") || lower.Contains("ok");
        }

        // ────────────────────────────────────────────────────────────────────
        //  ERRORES
        // ────────────────────────────────────────────────────────────────────

        private void LoadErrorDetails()
        {
            if (isClosing) return;
            var data = new List<dynamic>();
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("SELECT * FROM dbo.ConsolidationEngineErrorsView", conn))
                {
                    cmd.CommandTimeout = 30;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(new
                            {
                                ID = GetSafeValue(reader, "ID"),
                                SourceDatabase = GetSafeString(reader, "SourceDatabase"),
                                TableName = GetSafeString(reader, "TableName"),
                                ErrorMessage = GetSafeString(reader, "ErrorDetails"),
                                CreatedAt = GetSafeString(reader, "CreatedAt")
                            });
                        }
                    }
                }
                UpdateGridSafe(DetalleErroresGrid, () => UpdateErrorGrid(data));
            }
            catch (SqlException ex) { LogError("LoadErrorDetails - SQL Error", ex); UpdateGridSafe(DetalleErroresGrid, () => ShowGridError(DetalleErroresGrid, "Error de base de datos al cargar errores")); }
            catch (Exception ex) { LogError("LoadErrorDetails", ex); UpdateGridSafe(DetalleErroresGrid, () => ShowGridError(DetalleErroresGrid, "Error al cargar detalles de errores")); }
        }

        private void UpdateErrorGrid(List<dynamic> data)
        {
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
                DetalleErroresGrid.Rows.Add(item.SourceDatabase, item.TableName, item.ErrorMessage, item.CreatedAt, item.ID);
        }

        // ────────────────────────────────────────────────────────────────────
        //  LOGS
        // ────────────────────────────────────────────────────────────────────

        private void LoadLogs()
        {
            if (isClosing) return;
            var data = new List<Dictionary<string, object>>();
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("SELECT * FROM dbo.ConsolidationEngineLogsView", conn))
                {
                    cmd.CommandTimeout = 30;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            data.Add(row);
                        }
                    }
                }
                UpdateGridSafe(logsGrid, () => UpdateLogsGrid(data));
            }
            catch (SqlException ex) { LogError("LoadLogs - SQL Error", ex); UpdateGridSafe(logsGrid, () => ShowGridError(logsGrid, "Error de base de datos al cargar logs")); }
            catch (Exception ex) { LogError("LoadLogs", ex); UpdateGridSafe(logsGrid, () => ShowGridError(logsGrid, "Error al cargar logs")); }
        }

        private void UpdateLogsGrid(List<Dictionary<string, object>> data)
        {
            logsGrid.DataSource = null;
            logsGrid.Rows.Clear();
            logsGrid.Columns.Clear();
            if (data.Count > 0)
            {
                foreach (var key in data[0].Keys)
                    logsGrid.Columns.Add(key, key);
                foreach (var row in data)
                    logsGrid.Rows.Add(row.Values.ToArray());
            }
        }

        // ────────────────────────────────────────────────────────────────────
        //  ESTILO DE SECCIONES (Errores y Logs)
        // ────────────────────────────────────────────────────────────────────

        private void StyleSectionPanels()
        {
            StyleSection(
                detalleErroresGroup,
                DetalleErroresGrid,
                "✖   Detalle de errores",
                ColorError,
                showRetryButton: true);

            StyleSection(
                logsGroup,
                logsGrid,
                "≡   Logs de actividad",
                Color.FromArgb(41, 128, 185),
                showRetryButton: false);
        }

        private void StyleSection(GroupBox group, DataGridView grid,
            string title, Color accentColor, bool showRetryButton)
        {
            // ── Ocultar el GroupBox nativo y usarlo solo como contenedor lógico
            group.ForeColor  = Color.Transparent;
            group.Font       = new Font("Segoe UI", 1F);
            group.BackColor  = Color.Transparent;
            group.FlatStyle  = FlatStyle.Flat;

            // ── Panel contenedor con borde redondeado ─────────────────────────
            var container = new Panel
            {
                Location  = group.Location,
                Size      = group.Size,
                BackColor = SectionBg,
                Parent    = mainPanel
            };
            container.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(SectionBorder, 1))
                using (var path = GetRoundedRect(new Rectangle(0, 0, container.Width - 1, container.Height - 1), 8))
                    pe.Graphics.DrawPath(pen, path);
            };

            // ── Header de la sección (ocupa todo el ancho, sin barra lateral) ─
            var header = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(container.Width, 42),
                BackColor = SectionHeaderBg,
                Parent    = container
            };

            var titleLabel = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = SectionHeaderText,
                Location  = new Point(14, 0),
                Size      = new Size(container.Width - 200, 42),
                TextAlign = ContentAlignment.MiddleLeft,
                Parent    = header
            };

            // ── Grid reubicado dentro del nuevo contenedor ────────────────────
            grid.Parent   = container;
            grid.Location = new Point(5, 50);
            grid.Size     = new Size(container.Width - 10, container.Height - 60);

            // ── Botón Reintentar (solo para errores) ──────────────────────────
            if (showRetryButton)
            {
                grid.Size = new Size(container.Width - 10, container.Height - 102);

                btnRetryErrors.Parent   = container;
                btnRetryErrors.Location = new Point(5, container.Height - 44);
                btnRetryErrors.Size     = new Size(220, 36);
                btnRetryErrors.Text     = "↻   Reintentar todos los errores";
                btnRetryErrors.FlatStyle = FlatStyle.Flat;
                btnRetryErrors.UseVisualStyleBackColor = false;
                btnRetryErrors.BackColor = ColorError;
                btnRetryErrors.ForeColor = Color.White;
                btnRetryErrors.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                btnRetryErrors.FlatAppearance.BorderSize = 0;
                btnRetryErrors.Cursor    = Cursors.Hand;

                btnRetryErrors.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var brush = new SolidBrush(btnRetryErrors.BackColor))
                    using (var path  = GetRoundedRect(new Rectangle(0, 0, btnRetryErrors.Width - 1, btnRetryErrors.Height - 1), 8))
                        pe.Graphics.FillPath(brush, path);
                    var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
                    TextRenderer.DrawText(pe.Graphics, btnRetryErrors.Text, btnRetryErrors.Font,
                        new Rectangle(0, 0, btnRetryErrors.Width, btnRetryErrors.Height), btnRetryErrors.ForeColor, flags);
                };
                btnRetryErrors.MouseEnter += (s, e2) => { btnRetryErrors.BackColor = Color.FromArgb(160, 40, 30); btnRetryErrors.Invalidate(); };
                btnRetryErrors.MouseLeave += (s, e2) => { btnRetryErrors.BackColor = ColorError; btnRetryErrors.Invalidate(); };
            }

            container.BringToFront();
        }

        // ────────────────────────────────────────────────────────────────────
        //  HELPERS
        // ────────────────────────────────────────────────────────────────────

        private void UpdateGridSafe(DataGridView grid, Action updateAction)
        {
            if (grid == null || isClosing) return;
            if (grid.InvokeRequired) { grid.Invoke(new Action(() => UpdateGridSafe(grid, updateAction))); return; }
            try { updateAction(); }
            catch (Exception ex) { LogError("UpdateGridSafe", ex); }
        }

        private void ShowGridError(DataGridView grid, string errorMessage)
        {
            grid.DataSource = null;
            grid.Rows.Clear();
            grid.Columns.Clear();
            grid.Columns.Add("Error", "Error");
            grid.Rows.Add(errorMessage);
        }

        private string GetSafeString(SqlDataReader reader, string columnName)
        {
            try { int o = reader.GetOrdinal(columnName); return reader.IsDBNull(o) ? "" : reader.GetValue(o).ToString(); }
            catch { return ""; }
        }

        private int GetSafeInt(SqlDataReader reader, string columnName)
        {
            try { int o = reader.GetOrdinal(columnName); return reader.IsDBNull(o) ? 0 : reader.GetInt32(o); }
            catch { return 0; }
        }

        private object GetSafeValue(SqlDataReader reader, string columnName)
        {
            try { int o = reader.GetOrdinal(columnName); return reader.IsDBNull(o) ? null : reader.GetValue(o); }
            catch { return null; }
        }

        private void RetryAllErrors()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["ConsolidationDb"].ConnectionString;
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("dbo.ConsolidationEngineRetryAll", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 60;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetStatus("Reintentos procesados exitosamente", ColorOk);
                LoadErrorDetails();
            }
            catch (SqlException ex) { LogError("RetryAllErrors - SQL Error", ex); ShowErrorMessage("Error al reintentar errores", ex); }
            catch (Exception ex) { LogError("RetryAllErrors", ex); ShowErrorMessage("Error al reintentar errores", ex); }
        }

        // ────────────────────────────────────────────────────────────────────
        //  EVENTOS
        // ────────────────────────────────────────────────────────────────────

        private void btnRetryErrors_Click(object sender, EventArgs e)
        {
            if (isLoading) { MessageBox.Show("Hay una operación en curso. Por favor espere.", "Operación en curso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show("¿Está seguro que desea reintentar todos los errores?", "Confirmar reintento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                btnRetryErrors.Enabled = false;
                SetStatus("Reintentando errores...", Color.FromArgb(52, 152, 219));
                try { RetryAllErrors(); }
                finally { btnRetryErrors.Enabled = true; }
            }
        }

        private void btnActualizar_Click(object sender, EventArgs e)
        {
            if (isLoading) { MessageBox.Show("Ya hay una actualización en curso. Por favor espere.", "Actualización en curso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            var scrollPos = scrollContainer.AutoScrollPosition;
            mainPanel.Focus();

            btnActualizar.Enabled = false;
            refreshTimer.Stop();
            try { RefreshAllData(); }
            finally
            {
                refreshTimer.Start();
                btnActualizar.Enabled = true;
                scrollContainer.AutoScrollPosition = new Point(-scrollPos.X, -scrollPos.Y);
            }
        }

        private void ShowErrorMessage(string title, Exception ex)
        {
            if (isClosing) return;
            if (InvokeRequired) { Invoke(new Action(() => ShowErrorMessage(title, ex))); return; }
            MessageBox.Show($"{title}\n\nError: {ex?.Message ?? "Error desconocido"}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void LogError(string context, Exception ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dashboard_errors.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex?.ToString() ?? "No exception details"}\n");
            }
            catch { }
        }

        private void DetalleErroresGrid_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dashboardTitle_Click(object sender, EventArgs e) { }
    }

    // ── Modelo simple para los datos de cada BD ──────────────────────────────
    internal class DbStatusItem
    {
        public string SourceDB { get; set; }
        public string LocalVersion { get; set; }
        public string ConsolidadaVersion { get; set; }
        public string Estado { get; set; }
        public int Errores { get; set; }
        public string UltimoError { get; set; }
    }

    // ── Estado calculado de cada BD ───────────────────────────────────────────
    internal enum DbSyncState { Ok, Syncing, Error }
}
