using Sample;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SampleForm
{
    public partial class LoginForm : Form
    {
        Dictionary<string, string> m_HostList = new Dictionary<string, string>();


        public LoginForm()
        {
            InitializeComponent();
            m_HostList["OPC Hosted Instance"] = "https://uacloudlibrary.opcfoundation.org/";
#if DEBUG
            m_HostList["Debug"] = "https://localhost:44388";
#endif
            foreach (string key in m_HostList.Keys)
            {
                HostComboBox.Items.Add(key);
            };
        }

        private void HostComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            HostTextBox.Text = m_HostList[HostComboBox.Text];
        }

        private void CancelLoginBtn_Click(object sender, EventArgs e)
        {
            UsernameTextBox.Text = "";
            PasswordTextBox.Text = "";
            HostTextBox.Text = "";
        }

        private void ContinueLoginBtn_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(UsernameTextBox.Text) && !string.IsNullOrEmpty(PasswordTextBox.Text) && !string.IsNullOrEmpty(HostTextBox.Text))
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Please fill every field");
                this.DialogResult = DialogResult.None;
            }
        }
    }
}
