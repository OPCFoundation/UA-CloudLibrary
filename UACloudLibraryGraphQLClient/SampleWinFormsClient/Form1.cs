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

            BrowserPanel.Enabled = false;
            FilePanel.Enabled = false;
            DownloadBtn.Enabled = true;

            LoginForm form = new LoginForm();
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
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }

            DataGridMethods.CreateCombinedView(ResultView);
        }

        private async void SearchBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var test = await client.GetCombinedResult();
                DataGridMethods.FillCombinedView(ResultView, test);
            }
            catch (Exception f)
            {
                MessageBox.Show(f.Message, "Error");
            }
        }

        private void DownloadBtn_Click(object sender, EventArgs e)
        {
            Download();
        }

        private async void Download()
        {
            int currentRow = ResultView.CurrentCell.RowIndex;
            if (currentRow >= 0)
            {
                try
                {
                    downloaded = await client.DownloadNodeset(DataGridMethods.GetNodesetId(currentRow).ToString());
                    if (!string.IsNullOrEmpty(downloaded.Nodeset.NodesetXml))
                    {
                        NodesetResultTextBox.Text = downloaded.Nodeset.NodesetXml;
                        FilePanel.Enabled = true;
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        private void ResultView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            Download();
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
