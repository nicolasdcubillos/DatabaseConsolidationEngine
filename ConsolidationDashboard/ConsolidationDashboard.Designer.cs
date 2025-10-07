namespace ConsolidationDashboard
{
    partial class ConsolidationDashboard
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dashboardTitle = new System.Windows.Forms.Label();
            this.GeneralDashboard = new System.Windows.Forms.DataGridView();
            this.DetalleErroresGrid = new System.Windows.Forms.DataGridView();
            this.resumenGeneralGroup = new System.Windows.Forms.GroupBox();
            this.detalleErroresGroup = new System.Windows.Forms.GroupBox();
            this.logsGroup = new System.Windows.Forms.GroupBox();
            this.logsGrid = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.GeneralDashboard)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DetalleErroresGrid)).BeginInit();
            this.resumenGeneralGroup.SuspendLayout();
            this.detalleErroresGroup.SuspendLayout();
            this.logsGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logsGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // dashboardTitle
            // 
            this.dashboardTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.dashboardTitle.Location = new System.Drawing.Point(0, 10);
            this.dashboardTitle.Name = "dashboardTitle";
            this.dashboardTitle.Size = new System.Drawing.Size(1600, 50);
            this.dashboardTitle.TabIndex = 1;
            this.dashboardTitle.Text = "Dashboard de consolidación";
            this.dashboardTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.dashboardTitle.Click += new System.EventHandler(this.dashboardTitle_Click);
            // 
            // GeneralDashboard
            // 
            this.GeneralDashboard.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.GeneralDashboard.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.GeneralDashboard.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.GeneralDashboard.Location = new System.Drawing.Point(25, 31);
            this.GeneralDashboard.Name = "GeneralDashboard";
            this.GeneralDashboard.RowHeadersVisible = false;
            this.GeneralDashboard.RowTemplate.Height = 30;
            this.GeneralDashboard.Size = new System.Drawing.Size(1500, 184);
            this.GeneralDashboard.TabIndex = 0;
            // Column header style
            System.Windows.Forms.DataGridViewCellStyle gridHeaderStyle = new System.Windows.Forms.DataGridViewCellStyle();
            gridHeaderStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            gridHeaderStyle.BackColor = System.Drawing.SystemColors.Control;
            gridHeaderStyle.Font = new System.Drawing.Font("Segoe UI", 10F);
            gridHeaderStyle.ForeColor = System.Drawing.SystemColors.WindowText;
            gridHeaderStyle.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            gridHeaderStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            gridHeaderStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            gridHeaderStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.GeneralDashboard.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.GeneralDashboard.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            // Default cell style
            System.Windows.Forms.DataGridViewCellStyle gridDefaultStyle = new System.Windows.Forms.DataGridViewCellStyle();
            gridDefaultStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            gridDefaultStyle.BackColor = System.Drawing.SystemColors.Window;
            gridDefaultStyle.Font = new System.Drawing.Font("Segoe UI", 10F);
            gridDefaultStyle.ForeColor = System.Drawing.SystemColors.ControlText;
            gridDefaultStyle.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            gridDefaultStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            gridDefaultStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            gridDefaultStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.GeneralDashboard.DefaultCellStyle = gridDefaultStyle;
            this.GeneralDashboard.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GeneralDashboard_CellContentClick);
            // 
            // DetalleErroresGrid
            // 
            this.DetalleErroresGrid.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.DetalleErroresGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.DetalleErroresGrid.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.DetalleErroresGrid.Location = new System.Drawing.Point(25, 31);
            this.DetalleErroresGrid.Name = "DetalleErroresGrid";
            this.DetalleErroresGrid.RowHeadersVisible = false;
            this.DetalleErroresGrid.RowTemplate.Height = 30;
            this.DetalleErroresGrid.Size = new System.Drawing.Size(1500, 184);
            this.DetalleErroresGrid.TabIndex = 0;
            this.DetalleErroresGrid.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.DetalleErroresGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DetalleErroresGrid.DefaultCellStyle = gridDefaultStyle;
            this.DetalleErroresGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DetalleErroresGrid_CellContentClick);
            // 
            // resumenGeneralGroup
            // 
            this.resumenGeneralGroup.Controls.Add(this.GeneralDashboard);
            this.resumenGeneralGroup.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.resumenGeneralGroup.Location = new System.Drawing.Point(25, 70);
            this.resumenGeneralGroup.Name = "resumenGeneralGroup";
            this.resumenGeneralGroup.Size = new System.Drawing.Size(1550, 234);
            this.resumenGeneralGroup.TabIndex = 2;
            this.resumenGeneralGroup.TabStop = false;
            this.resumenGeneralGroup.Text = "Resumen general";
            // 
            // detalleErroresGroup
            // 
            this.detalleErroresGroup.Controls.Add(this.DetalleErroresGrid);
            this.detalleErroresGroup.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.detalleErroresGroup.Location = new System.Drawing.Point(25, 310);
            this.detalleErroresGroup.Name = "detalleErroresGroup";
            this.detalleErroresGroup.Size = new System.Drawing.Size(1550, 234);
            this.detalleErroresGroup.TabIndex = 3;
            this.detalleErroresGroup.TabStop = false;
            this.detalleErroresGroup.Text = "Detalle de errores";
            // 
            // logsGroup
            // 
            this.logsGroup.Controls.Add(this.logsGrid);
            this.logsGroup.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.logsGroup.Location = new System.Drawing.Point(25, 550);
            this.logsGroup.Name = "logsGroup";
            this.logsGroup.Size = new System.Drawing.Size(1550, 245);
            this.logsGroup.TabIndex = 4;
            this.logsGroup.TabStop = false;
            this.logsGroup.Text = "Logs";
            // 
            // logsGrid
            // 
            this.logsGrid.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.logsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.logsGrid.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.logsGrid.Location = new System.Drawing.Point(25, 31);
            this.logsGrid.Name = "logsGrid";
            this.logsGrid.RowHeadersVisible = false;
            this.logsGrid.RowTemplate.Height = 30;
            this.logsGrid.Size = new System.Drawing.Size(1500, 184);
            this.logsGrid.TabIndex = 0;
            this.logsGrid.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.logsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.logsGrid.DefaultCellStyle = gridDefaultStyle;
            // 
            // ConsolidationDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 796);
            this.Controls.Add(this.dashboardTitle);
            this.Controls.Add(this.resumenGeneralGroup);
            this.Controls.Add(this.detalleErroresGroup);
            this.Controls.Add(this.logsGroup);
            this.Name = "ConsolidationDashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.GeneralDashboard)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DetalleErroresGrid)).EndInit();
            this.resumenGeneralGroup.ResumeLayout(false);
            this.detalleErroresGroup.ResumeLayout(false);
            this.logsGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.logsGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView GeneralDashboard;
        private System.Windows.Forms.DataGridView DetalleErroresGrid;
        private System.Windows.Forms.Label dashboardTitle;
        private System.Windows.Forms.GroupBox resumenGeneralGroup;
        private System.Windows.Forms.GroupBox detalleErroresGroup;
        private System.Windows.Forms.GroupBox logsGroup;
        private System.Windows.Forms.DataGridView logsGrid;
    }
}