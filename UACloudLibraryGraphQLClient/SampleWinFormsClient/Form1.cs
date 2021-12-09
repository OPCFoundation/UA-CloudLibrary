using SampleForm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UACloudLibClientLibrary;

namespace Sample
{
    
    public partial class Form1 : Form
    {
        public int m_SelectedAddressSpaceID;
        UACloudLibClient client = null;
        private AddressSpace downloaded;

        public Form1()
        {
            InitializeComponent();

            LoginForm form = new LoginForm();
            BrowserPanel.Enabled = true;
            FilePanel.Enabled = true;
            DownloadBtn.Enabled = true;

            if (form.ShowDialog() == DialogResult.OK)
            {
                string endpoint = form.HostTextBox.Text;
                string username = form.UsernameTextBox.Text;
                string password = form.PasswordTextBox.Text;
                form.Close();
                client = new UACloudLibClient(endpoint, username, password);
                BrowserPanel.Enabled = true;
            }
            else
            {
                this.Close();
            }

            DataGridMethods.CreateCombinedView(ResultView);
        }

        private async void SearchBtn_Click(object sender, EventArgs e)
        {
            var test = await client.GetCombinedResult();
            DataGridMethods.FillCombinedView(ResultView, test);
        }

        private async void DownloadBtn_Click(object sender, EventArgs e)
        {
            downloaded = await client.DownloadNodeset(DataGridMethods.GetNodesetId(ResultView.CurrentCell.RowIndex).ToString());
            NodesetResultTextBox.Text = downloaded.Nodeset.NodesetXml;
        }

        private async void ResultView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int currentRow = ResultView.CurrentCell.RowIndex;
            await client.DownloadNodeset(DataGridMethods.GetNodesetId(currentRow).ToString());
            NodesetResultTextBox.Text = downloaded.Nodeset.NodesetXml;
        }

        private void CrossBtn_Click(object sender, EventArgs e)
        {
            NodesetResultTextBox.Text = "";
            FilePanel.Visible = false;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            var maxwidth = this.Width;
            BrowserPanel.Width = (int)(maxwidth / 2) - (int)(maxwidth * 0.01);
            FilePanel.Width = (int)(maxwidth / 2) - (int)(maxwidth * 0.01);
            FilePanel.Location = new Point(x: (BrowserPanel.Location.X + BrowserPanel.Width), y: BrowserPanel.Location.Y);
            ResultView.AutoResizeColumns();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.FileName = (downloaded.Title + ".xml");
            fileDialog.ShowDialog();
        }
    }
}
