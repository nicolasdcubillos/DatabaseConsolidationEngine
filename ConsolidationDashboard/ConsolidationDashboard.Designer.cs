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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dashboardTitle = new System.Windows.Forms.Label();
            this.pieChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.GeneralDashboard = new System.Windows.Forms.DataGridView();
            this.DetalleErroresGrid = new System.Windows.Forms.DataGridView();
            this.resumenGeneralGroup = new System.Windows.Forms.GroupBox();
            this.detalleErroresGroup = new System.Windows.Forms.GroupBox();
            this.logsGroup = new System.Windows.Forms.GroupBox();
            this.logsGrid = new System.Windows.Forms.DataGridView();
            this.mainPanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pieChart)).BeginInit();
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
            this.dashboardTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.dashboardTitle.Location = new System.Drawing.Point(0, 10);
            this.dashboardTitle.Name = "dashboardTitle";
            this.dashboardTitle.Size = new System.Drawing.Size(1600, 50);
            this.dashboardTitle.TabIndex = 1;
            this.dashboardTitle.Text = "Monitoreo de Consolidación de bases de datos";
            this.dashboardTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.dashboardTitle.Click += new System.EventHandler(this.dashboardTitle_Click);
            // 
            // pieChart
            // 
            chartArea1.Name = "ChartArea1";
            this.pieChart.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.pieChart.Legends.Add(legend1);
            this.pieChart.Location = new System.Drawing.Point(601, 63);
            this.pieChart.Name = "pieChart";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Pie;
            series1.Legend = "Legend1";
            series1.Name = "ConsolidationStatus";
            this.pieChart.Series.Add(series1);
            this.pieChart.Size = new System.Drawing.Size(400, 250);
            this.pieChart.TabIndex = 10;
            this.pieChart.Text = "pieChart";
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
            this.GeneralDashboard.Location = new System.Drawing.Point(25, 31);
            this.GeneralDashboard.Name = "GeneralDashboard";
            this.GeneralDashboard.RowHeadersVisible = false;
            this.GeneralDashboard.RowTemplate.Height = 30;
            this.GeneralDashboard.Size = new System.Drawing.Size(1500, 184);
            this.GeneralDashboard.TabIndex = 0;
            this.GeneralDashboard.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GeneralDashboard_CellContentClick);
            // 
            // DetalleErroresGrid
            // 
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
            this.DetalleErroresGrid.Size = new System.Drawing.Size(1500, 184);
            this.DetalleErroresGrid.TabIndex = 0;
            this.DetalleErroresGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DetalleErroresGrid_CellContentClick);
            // 
            // resumenGeneralGroup
            // 
            this.resumenGeneralGroup.Controls.Add(this.GeneralDashboard);
            this.resumenGeneralGroup.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.resumenGeneralGroup.Location = new System.Drawing.Point(25, 326);
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
            this.detalleErroresGroup.Location = new System.Drawing.Point(25, 566);
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
            this.logsGroup.Location = new System.Drawing.Point(25, 806);
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
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.TabIndex = 100;
            // 
            // ConsolidationDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 796);
            this.Controls.Add(this.mainPanel);
            this.mainPanel.Controls.Add(this.dashboardTitle);
            this.mainPanel.Controls.Add(this.pieChart);
            this.mainPanel.Controls.Add(this.resumenGeneralGroup);
            this.mainPanel.Controls.Add(this.detalleErroresGroup);
            this.mainPanel.Controls.Add(this.logsGroup);
            this.Name = "ConsolidationDashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Monitoreo de Consolidacion";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pieChart)).EndInit();
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
        private System.Windows.Forms.DataVisualization.Charting.Chart pieChart;
        private System.Windows.Forms.Panel mainPanel;
    }
}