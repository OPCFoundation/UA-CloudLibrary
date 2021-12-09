
namespace SampleForm
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LoginPanel = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.ContinueLoginBtn = new System.Windows.Forms.Button();
            this.HostTextBox = new System.Windows.Forms.TextBox();
            this.CancelLoginBtn = new System.Windows.Forms.Button();
            this.UsernameTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.PasswordTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.HostComboBox = new System.Windows.Forms.ComboBox();
            this.LoginPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // LoginPanel
            // 
            this.LoginPanel.Controls.Add(this.label1);
            this.LoginPanel.Controls.Add(this.ContinueLoginBtn);
            this.LoginPanel.Controls.Add(this.HostTextBox);
            this.LoginPanel.Controls.Add(this.CancelLoginBtn);
            this.LoginPanel.Controls.Add(this.UsernameTextBox);
            this.LoginPanel.Controls.Add(this.label3);
            this.LoginPanel.Controls.Add(this.PasswordTextBox);
            this.LoginPanel.Controls.Add(this.label2);
            this.LoginPanel.Controls.Add(this.HostComboBox);
            this.LoginPanel.Location = new System.Drawing.Point(12, 12);
            this.LoginPanel.Name = "LoginPanel";
            this.LoginPanel.Size = new System.Drawing.Size(335, 187);
            this.LoginPanel.TabIndex = 15;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "Host";
            // 
            // ContinueLoginBtn
            // 
            this.ContinueLoginBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ContinueLoginBtn.Location = new System.Drawing.Point(257, 161);
            this.ContinueLoginBtn.Name = "ContinueLoginBtn";
            this.ContinueLoginBtn.Size = new System.Drawing.Size(75, 23);
            this.ContinueLoginBtn.TabIndex = 8;
            this.ContinueLoginBtn.Text = "Login";
            this.ContinueLoginBtn.UseVisualStyleBackColor = true;
            this.ContinueLoginBtn.Click += new System.EventHandler(this.ContinueLoginBtn_Click);
            // 
            // HostTextBox
            // 
            this.HostTextBox.Location = new System.Drawing.Point(96, 48);
            this.HostTextBox.Name = "HostTextBox";
            this.HostTextBox.Size = new System.Drawing.Size(236, 23);
            this.HostTextBox.TabIndex = 0;
            // 
            // CancelLoginBtn
            // 
            this.CancelLoginBtn.Location = new System.Drawing.Point(3, 161);
            this.CancelLoginBtn.Name = "CancelLoginBtn";
            this.CancelLoginBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelLoginBtn.TabIndex = 7;
            this.CancelLoginBtn.Text = "Cancel";
            this.CancelLoginBtn.UseVisualStyleBackColor = true;
            this.CancelLoginBtn.Click += new System.EventHandler(this.CancelLoginBtn_Click);
            // 
            // UsernameTextBox
            // 
            this.UsernameTextBox.Location = new System.Drawing.Point(96, 77);
            this.UsernameTextBox.Name = "UsernameTextBox";
            this.UsernameTextBox.Size = new System.Drawing.Size(236, 23);
            this.UsernameTextBox.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 106);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 15);
            this.label3.TabIndex = 6;
            this.label3.Text = "Password";
            // 
            // PasswordTextBox
            // 
            this.PasswordTextBox.Location = new System.Drawing.Point(96, 106);
            this.PasswordTextBox.Name = "PasswordTextBox";
            this.PasswordTextBox.PasswordChar = '*';
            this.PasswordTextBox.Size = new System.Drawing.Size(236, 23);
            this.PasswordTextBox.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 15);
            this.label2.TabIndex = 5;
            this.label2.Text = "Username";
            // 
            // HostComboBox
            // 
            this.HostComboBox.FormattingEnabled = true;
            this.HostComboBox.Location = new System.Drawing.Point(96, 19);
            this.HostComboBox.Name = "HostComboBox";
            this.HostComboBox.Size = new System.Drawing.Size(236, 23);
            this.HostComboBox.TabIndex = 3;
            this.HostComboBox.Text = "Host List";
            this.HostComboBox.SelectedIndexChanged += new System.EventHandler(this.HostComboBox_SelectedIndexChanged);
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 211);
            this.Controls.Add(this.LoginPanel);
            this.Name = "LoginForm";
            this.Text = "Login Page Sample";
            this.LoginPanel.ResumeLayout(false);
            this.LoginPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel LoginPanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button ContinueLoginBtn;
        public System.Windows.Forms.TextBox HostTextBox;
        private System.Windows.Forms.Button CancelLoginBtn;
        public System.Windows.Forms.TextBox UsernameTextBox;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox PasswordTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox HostComboBox;
    }
}