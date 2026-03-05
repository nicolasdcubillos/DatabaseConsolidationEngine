namespace ConsolidationDashboard
{
    partial class ConsolidationDashboard
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dashboardTitle = new System.Windows.Forms.Label();
            this.kpiTotal = new System.Windows.Forms.Panel();
            this.kpiTotalValue = new System.Windows.Forms.Label();
            this.kpiTotalLabel = new System.Windows.Forms.Label();
            this.kpiOk = new System.Windows.Forms.Panel();
            this.kpiOkValue = new System.Windows.Forms.Label();
            this.kpiOkLabel = new System.Windows.Forms.Label();
            this.kpiError = new System.Windows.Forms.Panel();
            this.kpiErrorValue = new System.Windows.Forms.Label();
            this.kpiErrorLabel = new System.Windows.Forms.Label();
            this.btnActualizar = new System.Windows.Forms.Button();
            this.resumenGeneralGroup = new System.Windows.Forms.GroupBox();
            this.cardsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.detalleErroresGroup = new System.Windows.Forms.GroupBox();
            this.DetalleErroresGrid = new System.Windows.Forms.DataGridView();
            this.btnRetryErrors = new System.Windows.Forms.Button();
            this.logsGroup = new System.Windows.Forms.GroupBox();
            this.logsGrid = new System.Windows.Forms.DataGridView();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.scrollContainer = new System.Windows.Forms.Panel();

            ((System.ComponentModel.ISupportInitialize)(this.DetalleErroresGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.logsGrid)).BeginInit();
            this.kpiTotal.SuspendLayout();
            this.kpiOk.SuspendLayout();
            this.kpiError.SuspendLayout();
            this.resumenGeneralGroup.SuspendLayout();
            this.detalleErroresGroup.SuspendLayout();
            this.logsGroup.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.scrollContainer.SuspendLayout();
            this.SuspendLayout();

            // ── dashboardTitle ──────────────────────────────────────────────
            this.dashboardTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.dashboardTitle.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            this.dashboardTitle.Location = new System.Drawing.Point(0, 15);
            this.dashboardTitle.Name = "dashboardTitle";
            this.dashboardTitle.Size = new System.Drawing.Size(1600, 60);
            this.dashboardTitle.TabIndex = 1;
            this.dashboardTitle.Text = "Monitoreo - Consolidación de Bases de Datos";
            this.dashboardTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.dashboardTitle.Click += new System.EventHandler(this.dashboardTitle_Click);

            // ── KPI Total ────────────────────────────────────────────────────
            this.kpiTotalValue.AutoSize = false;
            this.kpiTotalValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiTotalValue.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold);
            this.kpiTotalValue.ForeColor = System.Drawing.Color.White;
            this.kpiTotalValue.Name = "kpiTotalValue";
            this.kpiTotalValue.Text = "—";
            this.kpiTotalValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.kpiTotalLabel.AutoSize = false;
            this.kpiTotalLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.kpiTotalLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.kpiTotalLabel.ForeColor = System.Drawing.Color.FromArgb(189, 195, 199);
            this.kpiTotalLabel.Height = 22;
            this.kpiTotalLabel.Name = "kpiTotalLabel";
            this.kpiTotalLabel.Text = "TOTAL BDs";
            this.kpiTotalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.kpiTotal.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.kpiTotal.Controls.Add(this.kpiTotalValue);
            this.kpiTotal.Controls.Add(this.kpiTotalLabel);
            this.kpiTotal.Location = new System.Drawing.Point(25, 88);
            this.kpiTotal.Name = "kpiTotal";
            this.kpiTotal.Size = new System.Drawing.Size(160, 70);
            this.kpiTotal.TabIndex = 20;

            // ── KPI OK ───────────────────────────────────────────────────────
            this.kpiOkValue.AutoSize = false;
            this.kpiOkValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiOkValue.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold);
            this.kpiOkValue.ForeColor = System.Drawing.Color.White;
            this.kpiOkValue.Name = "kpiOkValue";
            this.kpiOkValue.Text = "—";
            this.kpiOkValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.kpiOkLabel.AutoSize = false;
            this.kpiOkLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.kpiOkLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.kpiOkLabel.ForeColor = System.Drawing.Color.FromArgb(209, 255, 209);
            this.kpiOkLabel.Height = 22;
            this.kpiOkLabel.Name = "kpiOkLabel";
            this.kpiOkLabel.Text = "CONSOLIDADAS";
            this.kpiOkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.kpiOk.BackColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.kpiOk.Controls.Add(this.kpiOkValue);
            this.kpiOk.Controls.Add(this.kpiOkLabel);
            this.kpiOk.Location = new System.Drawing.Point(205, 88);
            this.kpiOk.Name = "kpiOk";
            this.kpiOk.Size = new System.Drawing.Size(160, 70);
            this.kpiOk.TabIndex = 21;

            // ── KPI Error ────────────────────────────────────────────────────
            this.kpiErrorValue.AutoSize = false;
            this.kpiErrorValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiErrorValue.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold);
            this.kpiErrorValue.ForeColor = System.Drawing.Color.White;
            this.kpiErrorValue.Name = "kpiErrorValue";
            this.kpiErrorValue.Text = "—";
            this.kpiErrorValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.kpiErrorLabel.AutoSize = false;
            this.kpiErrorLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.kpiErrorLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.kpiErrorLabel.ForeColor = System.Drawing.Color.FromArgb(255, 200, 200);
            this.kpiErrorLabel.Height = 22;
            this.kpiErrorLabel.Name = "kpiErrorLabel";
            this.kpiErrorLabel.Text = "CON ERRORES";
            this.kpiErrorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.kpiError.BackColor = System.Drawing.Color.FromArgb(192, 57, 43);
            this.kpiError.Controls.Add(this.kpiErrorValue);
            this.kpiError.Controls.Add(this.kpiErrorLabel);
            this.kpiError.Location = new System.Drawing.Point(385, 88);
            this.kpiError.Name = "kpiError";
            this.kpiError.Size = new System.Drawing.Size(160, 70);
            this.kpiError.TabIndex = 22;

            // ── btnActualizar ────────────────────────────────────────────────
            this.btnActualizar.Font     = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnActualizar.Location = new System.Drawing.Point(600, 88); // reasignado en InitializeHeaderRight
            this.btnActualizar.Name     = "btnActualizar";
            this.btnActualizar.Size     = new System.Drawing.Size(175, 70);  // reasignado en InitializeHeaderRight
            this.btnActualizar.TabIndex = 2;
            this.btnActualizar.TabStop  = false;
            this.btnActualizar.Text     = "🔄  Actualizar";
            this.btnActualizar.UseVisualStyleBackColor = false;
            this.btnActualizar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActualizar.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            this.btnActualizar.ForeColor = System.Drawing.Color.White;
            this.btnActualizar.FlatAppearance.BorderSize = 0;
            this.btnActualizar.Cursor   = System.Windows.Forms.Cursors.Hand;
            this.btnActualizar.Click   += new System.EventHandler(this.btnActualizar_Click);

            // ── cardsPanel ───────────────────────────────────────────────────
            this.cardsPanel.AutoScroll = false;
            this.cardsPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.cardsPanel.WrapContents = true;
            this.cardsPanel.Location = new System.Drawing.Point(10, 28);
            this.cardsPanel.Name = "cardsPanel";
            this.cardsPanel.Padding = new System.Windows.Forms.Padding(5);
            this.cardsPanel.Size = new System.Drawing.Size(1530, 700);
            this.cardsPanel.TabIndex = 0;

            // ── resumenGeneralGroup ──────────────────────────────────────────
            this.resumenGeneralGroup.Controls.Add(this.cardsPanel);
            this.resumenGeneralGroup.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.resumenGeneralGroup.Location = new System.Drawing.Point(25, 175);
            this.resumenGeneralGroup.Name = "resumenGeneralGroup";
            this.resumenGeneralGroup.Size = new System.Drawing.Size(1550, 740);
            this.resumenGeneralGroup.TabIndex = 2;
            this.resumenGeneralGroup.TabStop = false;
            this.resumenGeneralGroup.Text = "Resumen general";

            // ── DetalleErroresGrid ───────────────────────────────────────────
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 10F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;

            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 10F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;

            this.DetalleErroresGrid.AllowUserToAddRows = false;
            this.DetalleErroresGrid.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.DetalleErroresGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.DetalleErroresGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.DetalleErroresGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DetalleErroresGrid.DefaultCellStyle = dataGridViewCellStyle2;
            this.DetalleErroresGrid.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.DetalleErroresGrid.Location = new System.Drawing.Point(25, 31);
            this.DetalleErroresGrid.Name = "DetalleErroresGrid";
            this.DetalleErroresGrid.RowHeadersVisible = false;
            this.DetalleErroresGrid.RowTemplate.Height = 30;
            this.DetalleErroresGrid.Size = new System.Drawing.Size(1500, 170);
            this.DetalleErroresGrid.TabIndex = 0;
            this.DetalleErroresGrid.TabStop  = false;
            this.DetalleErroresGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DetalleErroresGrid_CellContentClick);

            // ── btnRetryErrors ───────────────────────────────────────────────
            this.btnRetryErrors.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnRetryErrors.Location = new System.Drawing.Point(25, 210);
            this.btnRetryErrors.Name = "btnRetryErrors";
            this.btnRetryErrors.Size = new System.Drawing.Size(200, 30);
            this.btnRetryErrors.TabIndex = 1;
            this.btnRetryErrors.TabStop  = false;
            this.btnRetryErrors.Text = "Reintentar todo";
            this.btnRetryErrors.UseVisualStyleBackColor = true;
            this.btnRetryErrors.Click += new System.EventHandler(this.btnRetryErrors_Click);

            // ── detalleErroresGroup ──────────────────────────────────────────
            this.detalleErroresGroup.Controls.Add(this.DetalleErroresGrid);
            this.detalleErroresGroup.Controls.Add(this.btnRetryErrors);
            this.detalleErroresGroup.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.detalleErroresGroup.Location = new System.Drawing.Point(25, 930);
            this.detalleErroresGroup.Name = "detalleErroresGroup";
            this.detalleErroresGroup.Size = new System.Drawing.Size(1550, 250);
            this.detalleErroresGroup.TabIndex = 3;
            this.detalleErroresGroup.TabStop = false;
            this.detalleErroresGroup.Text = "Detalle de errores";

            // ── logsGrid ─────────────────────────────────────────────────────
            this.logsGrid.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.logsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.logsGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.logsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.logsGrid.DefaultCellStyle = dataGridViewCellStyle2;
            this.logsGrid.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.logsGrid.Location = new System.Drawing.Point(25, 31);
            this.logsGrid.Name = "logsGrid";
            this.logsGrid.RowHeadersVisible = false;
            this.logsGrid.RowTemplate.Height = 30;
            this.logsGrid.Size = new System.Drawing.Size(1500, 184);
            this.logsGrid.TabIndex = 0;
            this.logsGrid.TabStop  = false;

            // ── logsGroup ────────────────────────────────────────────────────
            this.logsGroup.Controls.Add(this.logsGrid);
            this.logsGroup.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.logsGroup.Location = new System.Drawing.Point(25, 1195);
            this.logsGroup.Name = "logsGroup";
            this.logsGroup.Size = new System.Drawing.Size(1550, 245);
            this.logsGroup.TabIndex = 4;
            this.logsGroup.TabStop = false;
            this.logsGroup.Text = "Logs";

            // ── mainPanel ────────────────────────────────────────────────────
            this.mainPanel.AutoScroll = false;
            this.mainPanel.Controls.Add(this.dashboardTitle);
            this.mainPanel.Controls.Add(this.kpiTotal);
            this.mainPanel.Controls.Add(this.kpiOk);
            this.mainPanel.Controls.Add(this.kpiError);
            this.mainPanel.Controls.Add(this.btnActualizar);
            this.mainPanel.Controls.Add(this.resumenGeneralGroup);
            this.mainPanel.Controls.Add(this.detalleErroresGroup);
            this.mainPanel.Controls.Add(this.logsGroup);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.None;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1600, 1460);
            this.mainPanel.TabIndex = 100;

            // ── scrollContainer ──────────────────────────────────────────────
            this.scrollContainer.AutoScroll = true;
            this.scrollContainer.Controls.Add(this.mainPanel);
            this.scrollContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scrollContainer.Location = new System.Drawing.Point(0, 0);
            this.scrollContainer.Name = "scrollContainer";
            this.scrollContainer.Size = new System.Drawing.Size(1600, 900);
            this.scrollContainer.TabIndex = 0;

            // ── Form ─────────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Controls.Add(this.scrollContainer);
            this.Name = "ConsolidationDashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Monitoreo de Consolidacion";
            this.Load += new System.EventHandler(this.Form1_Load);

            ((System.ComponentModel.ISupportInitialize)(this.DetalleErroresGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.logsGrid)).EndInit();
            this.kpiTotal.ResumeLayout(false);
            this.kpiOk.ResumeLayout(false);
            this.kpiError.ResumeLayout(false);
            this.resumenGeneralGroup.ResumeLayout(false);
            this.detalleErroresGroup.ResumeLayout(false);
            this.logsGroup.ResumeLayout(false);
            this.mainPanel.ResumeLayout(false);
            this.scrollContainer.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label dashboardTitle;
        private System.Windows.Forms.Panel kpiTotal;
        private System.Windows.Forms.Label kpiTotalValue;
        private System.Windows.Forms.Label kpiTotalLabel;
        private System.Windows.Forms.Panel kpiOk;
        private System.Windows.Forms.Label kpiOkValue;
        private System.Windows.Forms.Label kpiOkLabel;
        private System.Windows.Forms.Panel kpiError;
        private System.Windows.Forms.Label kpiErrorValue;
        private System.Windows.Forms.Label kpiErrorLabel;
        private System.Windows.Forms.Button btnActualizar;
        private System.Windows.Forms.FlowLayoutPanel cardsPanel;
        private System.Windows.Forms.GroupBox resumenGeneralGroup;
        private System.Windows.Forms.DataGridView DetalleErroresGrid;
        private System.Windows.Forms.Button btnRetryErrors;
        private System.Windows.Forms.GroupBox detalleErroresGroup;
        private System.Windows.Forms.GroupBox logsGroup;
        private System.Windows.Forms.DataGridView logsGrid;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Panel scrollContainer;
    }
}