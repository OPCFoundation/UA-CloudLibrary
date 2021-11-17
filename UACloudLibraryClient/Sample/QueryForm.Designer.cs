
namespace Sample
{
    partial class QueryForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.FilePanel = new System.Windows.Forms.Panel();
            this.NodesetResultTextBox = new System.Windows.Forms.RichTextBox();
            this.LoadingBtn = new System.Windows.Forms.Button();
            this.SavingBtn = new System.Windows.Forms.Button();
            this.CrossBtn = new System.Windows.Forms.Button();
            this.BrowserPanel = new System.Windows.Forms.Panel();
            this.QueryComboBox = new System.Windows.Forms.ComboBox();
            this.NextPageBtn = new System.Windows.Forms.Button();
            this.PrevPageBtn = new System.Windows.Forms.Button();
            this.DownloadBtn = new System.Windows.Forms.Button();
            this.AddCriteriaBtn = new System.Windows.Forms.Button();
            this.SearchBtn = new System.Windows.Forms.Button();
            this.CriteriaComboBox = new System.Windows.Forms.ComboBox();
            this.ValueTextBox = new System.Windows.Forms.TextBox();
            this.SearchTextBox = new System.Windows.Forms.TextBox();
            this.ResultView = new System.Windows.Forms.DataGridView();
            this.FilePanel.SuspendLayout();
            this.BrowserPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ResultView)).BeginInit();
            this.SuspendLayout();
            // 
            // FilePanel
            // 
            this.FilePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FilePanel.BackColor = System.Drawing.Color.White;
            this.FilePanel.Controls.Add(this.NodesetResultTextBox);
            this.FilePanel.Controls.Add(this.LoadingBtn);
            this.FilePanel.Controls.Add(this.SavingBtn);
            this.FilePanel.Controls.Add(this.CrossBtn);
            this.FilePanel.Location = new System.Drawing.Point(913, 12);
            this.FilePanel.Name = "FilePanel";
            this.FilePanel.Size = new System.Drawing.Size(367, 609);
            this.FilePanel.TabIndex = 12;
            // 
            // NodesetResultTextBox
            // 
            this.NodesetResultTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NodesetResultTextBox.Location = new System.Drawing.Point(3, 4);
            this.NodesetResultTextBox.Name = "NodesetResultTextBox";
            this.NodesetResultTextBox.Size = new System.Drawing.Size(360, 573);
            this.NodesetResultTextBox.TabIndex = 25;
            this.NodesetResultTextBox.Text = "";
            // 
            // LoadingBtn
            // 
            this.LoadingBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LoadingBtn.Location = new System.Drawing.Point(116, 518);
            this.LoadingBtn.Name = "LoadingBtn";
            this.LoadingBtn.Size = new System.Drawing.Size(107, 23);
            this.LoadingBtn.TabIndex = 23;
            this.LoadingBtn.Text = "Loading...";
            this.LoadingBtn.UseVisualStyleBackColor = true;
            // 
            // SavingBtn
            // 
            this.SavingBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SavingBtn.Location = new System.Drawing.Point(3, 518);
            this.SavingBtn.Name = "SavingBtn";
            this.SavingBtn.Size = new System.Drawing.Size(107, 23);
            this.SavingBtn.TabIndex = 22;
            this.SavingBtn.Text = "Saving...";
            this.SavingBtn.UseVisualStyleBackColor = true;
            // 
            // CrossBtn
            // 
            this.CrossBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CrossBtn.Location = new System.Drawing.Point(339, 518);
            this.CrossBtn.Name = "CrossBtn";
            this.CrossBtn.Size = new System.Drawing.Size(24, 23);
            this.CrossBtn.TabIndex = 21;
            this.CrossBtn.UseVisualStyleBackColor = true;
            this.CrossBtn.Click += new System.EventHandler(this.CrossBtn_Click);
            // 
            // BrowserPanel
            // 
            this.BrowserPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BrowserPanel.Controls.Add(this.QueryComboBox);
            this.BrowserPanel.Controls.Add(this.NextPageBtn);
            this.BrowserPanel.Controls.Add(this.PrevPageBtn);
            this.BrowserPanel.Controls.Add(this.DownloadBtn);
            this.BrowserPanel.Controls.Add(this.AddCriteriaBtn);
            this.BrowserPanel.Controls.Add(this.SearchBtn);
            this.BrowserPanel.Controls.Add(this.CriteriaComboBox);
            this.BrowserPanel.Controls.Add(this.ValueTextBox);
            this.BrowserPanel.Controls.Add(this.SearchTextBox);
            this.BrowserPanel.Controls.Add(this.ResultView);
            this.BrowserPanel.Location = new System.Drawing.Point(12, 12);
            this.BrowserPanel.Name = "BrowserPanel";
            this.BrowserPanel.Size = new System.Drawing.Size(895, 609);
            this.BrowserPanel.TabIndex = 11;
            // 
            // QueryComboBox
            // 
            this.QueryComboBox.FormattingEnabled = true;
            this.QueryComboBox.Items.AddRange(new object[] {
            "AddressSpaces",
            "Organisations",
            "Categories"});
            this.QueryComboBox.Location = new System.Drawing.Point(4, 4);
            this.QueryComboBox.Name = "QueryComboBox";
            this.QueryComboBox.Size = new System.Drawing.Size(121, 23);
            this.QueryComboBox.TabIndex = 24;
            this.QueryComboBox.Text = "Type";
            this.QueryComboBox.SelectedIndexChanged += new System.EventHandler(this.QueryComboBox_SelectedIndexChanged);
            // 
            // NextPageBtn
            // 
            this.NextPageBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.NextPageBtn.Location = new System.Drawing.Point(85, 583);
            this.NextPageBtn.Name = "NextPageBtn";
            this.NextPageBtn.Size = new System.Drawing.Size(75, 23);
            this.NextPageBtn.TabIndex = 23;
            this.NextPageBtn.Text = "Next";
            this.NextPageBtn.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.NextPageBtn.UseVisualStyleBackColor = true;
            // 
            // PrevPageBtn
            // 
            this.PrevPageBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PrevPageBtn.Location = new System.Drawing.Point(4, 583);
            this.PrevPageBtn.Name = "PrevPageBtn";
            this.PrevPageBtn.Size = new System.Drawing.Size(75, 23);
            this.PrevPageBtn.TabIndex = 22;
            this.PrevPageBtn.Text = "Previous";
            this.PrevPageBtn.UseVisualStyleBackColor = true;
            // 
            // DownloadBtn
            // 
            this.DownloadBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DownloadBtn.Location = new System.Drawing.Point(817, 583);
            this.DownloadBtn.Name = "DownloadBtn";
            this.DownloadBtn.Size = new System.Drawing.Size(75, 23);
            this.DownloadBtn.TabIndex = 21;
            this.DownloadBtn.Text = "Download";
            this.DownloadBtn.UseVisualStyleBackColor = true;
            this.DownloadBtn.Click += new System.EventHandler(this.DownloadBtn_Click);
            // 
            // AddCriteriaBtn
            // 
            this.AddCriteriaBtn.Location = new System.Drawing.Point(361, 4);
            this.AddCriteriaBtn.Name = "AddCriteriaBtn";
            this.AddCriteriaBtn.Size = new System.Drawing.Size(81, 23);
            this.AddCriteriaBtn.TabIndex = 20;
            this.AddCriteriaBtn.Text = "Add Criteria";
            this.AddCriteriaBtn.UseVisualStyleBackColor = true;
            this.AddCriteriaBtn.Click += new System.EventHandler(this.AddCriteriaBtn_Click);
            // 
            // SearchBtn
            // 
            this.SearchBtn.Location = new System.Drawing.Point(485, 32);
            this.SearchBtn.Name = "SearchBtn";
            this.SearchBtn.Size = new System.Drawing.Size(75, 23);
            this.SearchBtn.TabIndex = 19;
            this.SearchBtn.Text = "Search";
            this.SearchBtn.UseVisualStyleBackColor = true;
            this.SearchBtn.Click += new System.EventHandler(this.SearchBtn_Click);
            // 
            // CriteriaComboBox
            // 
            this.CriteriaComboBox.FormattingEnabled = true;
            this.CriteriaComboBox.Location = new System.Drawing.Point(128, 4);
            this.CriteriaComboBox.Name = "CriteriaComboBox";
            this.CriteriaComboBox.Size = new System.Drawing.Size(121, 23);
            this.CriteriaComboBox.TabIndex = 18;
            this.CriteriaComboBox.Text = "Criteria";
            // 
            // ValueTextBox
            // 
            this.ValueTextBox.Location = new System.Drawing.Point(255, 4);
            this.ValueTextBox.Name = "ValueTextBox";
            this.ValueTextBox.PlaceholderText = "Value";
            this.ValueTextBox.Size = new System.Drawing.Size(100, 23);
            this.ValueTextBox.TabIndex = 17;
            // 
            // SearchTextBox
            // 
            this.SearchTextBox.Location = new System.Drawing.Point(128, 33);
            this.SearchTextBox.Name = "SearchTextBox";
            this.SearchTextBox.PlaceholderText = "Search...";
            this.SearchTextBox.Size = new System.Drawing.Size(351, 23);
            this.SearchTextBox.TabIndex = 11;
            // 
            // ResultView
            // 
            this.ResultView.AllowUserToDeleteRows = false;
            this.ResultView.AllowUserToOrderColumns = true;
            this.ResultView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ResultView.BackgroundColor = System.Drawing.SystemColors.HighlightText;
            this.ResultView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ResultView.Location = new System.Drawing.Point(4, 61);
            this.ResultView.MultiSelect = false;
            this.ResultView.Name = "ResultView";
            this.ResultView.ReadOnly = true;
            this.ResultView.RowTemplate.Height = 25;
            this.ResultView.Size = new System.Drawing.Size(888, 516);
            this.ResultView.TabIndex = 10;
            this.ResultView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ResultView_CellDoubleClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(1292, 623);
            this.Controls.Add(this.BrowserPanel);
            this.Controls.Add(this.FilePanel);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.HelpButton = true;
            this.Name = "Form1";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Text = "OPC UA Client Library Sample Form";
            this.FilePanel.ResumeLayout(false);
            this.BrowserPanel.ResumeLayout(false);
            this.BrowserPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ResultView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel FilePanel;
        private System.Windows.Forms.Button LoadingBtn;
        private System.Windows.Forms.Button SavingBtn;
        private System.Windows.Forms.Button CrossBtn;
        private System.Windows.Forms.Panel BrowserPanel;
        private System.Windows.Forms.ComboBox CriteriaComboBox;
        private System.Windows.Forms.TextBox ValueTextBox;
        private System.Windows.Forms.TextBox SearchTextBox;
        private System.Windows.Forms.DataGridView ResultView;
        private System.Windows.Forms.Button NextPageBtn;
        private System.Windows.Forms.Button PrevPageBtn;
        private System.Windows.Forms.Button DownloadBtn;
        private System.Windows.Forms.Button AddCriteriaBtn;
        private System.Windows.Forms.Button SearchBtn;
        private System.Windows.Forms.ComboBox QueryComboBox;
        private System.Windows.Forms.RichTextBox NodesetResultTextBox;
    }
}

