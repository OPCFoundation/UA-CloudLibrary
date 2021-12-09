
namespace Sample
{
    partial class Form1
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
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.NodesetResultTextBox = new System.Windows.Forms.RichTextBox();
            this.LoadingBtn = new System.Windows.Forms.Button();
            this.SavingBtn = new System.Windows.Forms.Button();
            this.CrossBtn = new System.Windows.Forms.Button();
            this.BrowserPanel = new System.Windows.Forms.Panel();
            this.DownloadBtn = new System.Windows.Forms.Button();
            this.SearchBtn = new System.Windows.Forms.Button();
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
            this.FilePanel.Controls.Add(this.btnBrowse);
            this.FilePanel.Controls.Add(this.txtFilePath);
            this.FilePanel.Controls.Add(this.NodesetResultTextBox);
            this.FilePanel.Controls.Add(this.LoadingBtn);
            this.FilePanel.Controls.Add(this.SavingBtn);
            this.FilePanel.Controls.Add(this.CrossBtn);
            this.FilePanel.Location = new System.Drawing.Point(913, 12);
            this.FilePanel.Name = "FilePanel";
            this.FilePanel.Size = new System.Drawing.Size(367, 609);
            this.FilePanel.TabIndex = 12;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(302, 582);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(61, 23);
            this.btnBrowse.TabIndex = 27;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // txtFilePath
            // 
            this.txtFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilePath.Location = new System.Drawing.Point(3, 583);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(293, 23);
            this.txtFilePath.TabIndex = 26;
            // 
            // NodesetResultTextBox
            // 
            this.NodesetResultTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
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
            this.BrowserPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.BrowserPanel.Controls.Add(this.DownloadBtn);
            this.BrowserPanel.Controls.Add(this.SearchBtn);
            this.BrowserPanel.Controls.Add(this.ResultView);
            this.BrowserPanel.Location = new System.Drawing.Point(12, 12);
            this.BrowserPanel.Name = "BrowserPanel";
            this.BrowserPanel.Size = new System.Drawing.Size(895, 609);
            this.BrowserPanel.TabIndex = 11;
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
            // SearchBtn
            // 
            this.SearchBtn.Location = new System.Drawing.Point(283, 582);
            this.SearchBtn.Name = "SearchBtn";
            this.SearchBtn.Size = new System.Drawing.Size(354, 23);
            this.SearchBtn.TabIndex = 19;
            this.SearchBtn.Text = "Search";
            this.SearchBtn.UseVisualStyleBackColor = true;
            this.SearchBtn.Click += new System.EventHandler(this.SearchBtn_Click);
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
            this.ResultView.Location = new System.Drawing.Point(4, 4);
            this.ResultView.MultiSelect = false;
            this.ResultView.Name = "ResultView";
            this.ResultView.ReadOnly = true;
            this.ResultView.RowTemplate.Height = 25;
            this.ResultView.Size = new System.Drawing.Size(888, 573);
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
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.FilePanel.ResumeLayout(false);
            this.FilePanel.PerformLayout();
            this.BrowserPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ResultView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel FilePanel;
        private System.Windows.Forms.Button LoadingBtn;
        private System.Windows.Forms.Button SavingBtn;
        private System.Windows.Forms.Button CrossBtn;
        private System.Windows.Forms.Panel BrowserPanel;
        private System.Windows.Forms.DataGridView ResultView;
        private System.Windows.Forms.Button DownloadBtn;
        private System.Windows.Forms.Button SearchBtn;
        private System.Windows.Forms.RichTextBox NodesetResultTextBox;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtFilePath;
    }
}

