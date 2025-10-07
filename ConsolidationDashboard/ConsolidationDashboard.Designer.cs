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
            this.resumenFinalGroup = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.GeneralDashboard)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DetalleErroresGrid)).BeginInit();
            this.resumenGeneralGroup.SuspendLayout();
            this.detalleErroresGroup.SuspendLayout();
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
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 10F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.GeneralDashboard.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.GeneralDashboard.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 10F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.GeneralDashboard.DefaultCellStyle = dataGridViewCellStyle2;
            this.GeneralDashboard.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.GeneralDashboard.Location = new System.Drawing.Point(25, 40);
            this.GeneralDashboard.Name = "GeneralDashboard";
            this.GeneralDashboard.RowHeadersVisible = false;
            this.GeneralDashboard.RowTemplate.Height = 25;
            this.GeneralDashboard.Size = new System.Drawing.Size(1500, 180);
            this.GeneralDashboard.TabIndex = 0;
            this.GeneralDashboard.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GeneralDashboard_CellContentClick);
            // 
            // DetalleErroresGrid
            // 
            this.DetalleErroresGrid.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.DetalleErroresGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 10F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DetalleErroresGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.DetalleErroresGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 10F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.DetalleErroresGrid.DefaultCellStyle = dataGridViewCellStyle4;
            this.DetalleErroresGrid.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.DetalleErroresGrid.Location = new System.Drawing.Point(25, 43);
            this.DetalleErroresGrid.Name = "DetalleErroresGrid";
            this.DetalleErroresGrid.RowHeadersVisible = false;
            this.DetalleErroresGrid.RowTemplate.Height = 25;
            this.DetalleErroresGrid.Size = new System.Drawing.Size(1500, 180);
            this.DetalleErroresGrid.TabIndex = 0;
            this.DetalleErroresGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DetalleErroresGrid_CellContentClick);
            // 
            // resumenGeneralGroup
            // 
            this.resumenGeneralGroup.Controls.Add(this.GeneralDashboard);
            this.resumenGeneralGroup.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.resumenGeneralGroup.Location = new System.Drawing.Point(25, 70);
            this.resumenGeneralGroup.Name = "resumenGeneralGroup";
            this.resumenGeneralGroup.Size = new System.Drawing.Size(1550, 250);
            this.resumenGeneralGroup.TabIndex = 2;
            this.resumenGeneralGroup.TabStop = false;
            this.resumenGeneralGroup.Text = "Resumen general";
            // 
            // detalleErroresGroup
            // 
            this.detalleErroresGroup.Controls.Add(this.DetalleErroresGrid);
            this.detalleErroresGroup.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.detalleErroresGroup.Location = new System.Drawing.Point(25, 330);
            this.detalleErroresGroup.Name = "detalleErroresGroup";
            this.detalleErroresGroup.Size = new System.Drawing.Size(1550, 237);
            this.detalleErroresGroup.TabIndex = 3;
            this.detalleErroresGroup.TabStop = false;
            this.detalleErroresGroup.Text = "Detalle de errores";
            // 
            // logsGroup
            // 
            this.logsGroup.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.logsGroup.Location = new System.Drawing.Point(25, 590);
            this.logsGroup.Name = "logsGroup";
            this.logsGroup.Size = new System.Drawing.Size(1550, 120);
            this.logsGroup.TabIndex = 4;
            this.logsGroup.TabStop = false;
            this.logsGroup.Text = "Logs";
            // 
            // resumenFinalGroup
            // 
            this.resumenFinalGroup.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.resumenFinalGroup.Location = new System.Drawing.Point(25, 720);
            this.resumenFinalGroup.Name = "resumenFinalGroup";
            this.resumenFinalGroup.Size = new System.Drawing.Size(1550, 120);
            this.resumenFinalGroup.TabIndex = 5;
            this.resumenFinalGroup.TabStop = false;
            this.resumenFinalGroup.Text = "Resumen consolidacion final";
            // 
            // ConsolidationDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Controls.Add(this.dashboardTitle);
            this.Controls.Add(this.resumenGeneralGroup);
            this.Controls.Add(this.detalleErroresGroup);
            this.Controls.Add(this.logsGroup);
            this.Controls.Add(this.resumenFinalGroup);
            this.Name = "ConsolidationDashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.GeneralDashboard)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DetalleErroresGrid)).EndInit();
            this.resumenGeneralGroup.ResumeLayout(false);
            this.detalleErroresGroup.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView GeneralDashboard;
        private System.Windows.Forms.DataGridView DetalleErroresGrid;
        private System.Windows.Forms.Label dashboardTitle;
        private System.Windows.Forms.GroupBox resumenGeneralGroup;
        private System.Windows.Forms.GroupBox detalleErroresGroup;
        private System.Windows.Forms.GroupBox logsGroup;
        private System.Windows.Forms.GroupBox resumenFinalGroup;
    }
}