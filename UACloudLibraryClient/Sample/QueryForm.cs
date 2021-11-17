using SampleForm;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using UACloudLibClientLibrary;
using UACloudLibClientLibrary.WhereExpressions;

namespace Sample
{
    public partial class QueryForm : Form
    {


        // Switch with factory pattern
        Tuple<string, PageInfo<AddressSpace>> m_AddressSpacePageInfo = null;
        Tuple<string, PageInfo<Organisation>> m_OrganisationPageInfo = null;
        Tuple<string, PageInfo<AddressSpaceCategory>> m_AddressSpaceCategoryPageInfo = null;
        List<AddressSpaceWhereExpression> AddressExpressions = new List<AddressSpaceWhereExpression>();
        List<OrganisationWhereExpression> OrgExpressions = new List<OrganisationWhereExpression>();
        List<CategoryWhereExpression> CategoryExpressions = new List<CategoryWhereExpression>();

        public int m_SelectedAddressSpaceID;
        UACloudLibClient client = null;

        public QueryForm()
        {
            InitializeComponent();

            LoginForm form = new LoginForm();
            BrowserPanel.Enabled = false;
            FilePanel.Enabled = false;

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
        }

        private void AddCriteriaBtn_Click(object sender, EventArgs e)
        {
            switch (QueryComboBox.SelectedItem)
            {
                case "AddressSpaces":
                    {
                        AddressSpaceWhereExpression ex = new AddressSpaceWhereExpression();
                        ex.Value = ValueTextBox.Text;
                        AddressSpaceSearchField field;
                        Enum.TryParse(CriteriaComboBox.Text, false, out field);
                        ex.Path = field;
                        CriteriaComboBox.Items.Remove(field.ToString());
                        AddressExpressions.Add(ex);
                        break;
                    }
                case "Organisations":
                    {
                        OrganisationWhereExpression ex = new OrganisationWhereExpression();
                        ex.Value = ValueTextBox.Text;
                        OrganisationSearchField field;
                        Enum.TryParse(CriteriaComboBox.Text, false, out field);
                        ex.Path = field;
                        OrgExpressions.Add(ex);
                        break;
                    }
                case "Categories":
                    {
                        CategoryWhereExpression ex = new CategoryWhereExpression();
                        ex.Value = ValueTextBox.Text;
                        CategorySearchField field;
                        Enum.TryParse(CriteriaComboBox.Text, false, out field);
                        ex.Path = field;
                        CategoryExpressions.Add(ex);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        private async void SearchBtn_Click(object sender, EventArgs e)
        {
            switch (QueryComboBox.SelectedItem)
            {
                case "AddressSpaces":
                    {
                        // request the addressspaces with given attributes and the data in the grid
                        m_AddressSpacePageInfo = await client.GetAddressSpaces(10, "-1", AddressExpressions);
                        if (!string.IsNullOrEmpty(m_AddressSpacePageInfo.Item1))
                        {
                            MessageBox.Show(m_AddressSpacePageInfo.Item1, "UA Cloud Library Client");
                            break;
                        }

                        DataGridMethods.FillAddressSpaceView(ResultView, m_AddressSpacePageInfo.Item2);

                        if ((m_AddressSpacePageInfo != null) && m_AddressSpacePageInfo.Item2 != null && (m_AddressSpacePageInfo.Item2.Page != null))
                        {
                            NextPageBtn.Visible = m_AddressSpacePageInfo.Item2.Page.hasNext;
                            PrevPageBtn.Visible = m_AddressSpacePageInfo.Item2.Page.hasPrev;
                        }

                        // remove attributes from list
                        AddressExpressions.Clear();
                        FillCriteriaComboBox(typeof(AddressSpaceSearchField));
                        break;
                    }
                case "Organisations":
                    {
                        m_OrganisationPageInfo = await client.GetOrganisations(10, "-1", OrgExpressions);
                        if (!string.IsNullOrEmpty(m_OrganisationPageInfo.Item1))
                        {
                            MessageBox.Show(m_OrganisationPageInfo.Item1, "UA Cloud Library Client");
                            break;
                        }

                        DataGridMethods.FillOrganisationView(ResultView, m_OrganisationPageInfo.Item2);

                        if ((m_OrganisationPageInfo != null) && (m_OrganisationPageInfo.Item2 != null) && (m_AddressSpaceCategoryPageInfo.Item2.Page != null))
                        {
                            NextPageBtn.Visible = m_OrganisationPageInfo.Item2.Page.hasNext;
                            PrevPageBtn.Visible = m_OrganisationPageInfo.Item2.Page.hasPrev;
                        }

                        OrgExpressions.Clear();
                        FillCriteriaComboBox(typeof(OrganisationSearchField));
                        break;
                    }
                case "Categories":
                    {
                        m_AddressSpaceCategoryPageInfo = await client.GetAddressSpaceCategories(10, "-1", CategoryExpressions);
                        if (!string.IsNullOrEmpty(m_AddressSpaceCategoryPageInfo.Item1))
                        {
                            MessageBox.Show(m_AddressSpaceCategoryPageInfo.Item1, "UA Cloud Library Client");
                            break;
                        }

                        DataGridMethods.FillCategoriesView(ResultView, m_AddressSpaceCategoryPageInfo.Item2);

                        if ((m_AddressSpaceCategoryPageInfo != null) && (m_AddressSpaceCategoryPageInfo.Item2 != null) && (m_AddressSpaceCategoryPageInfo.Item2.Page != null))
                        {
                            NextPageBtn.Visible = m_AddressSpaceCategoryPageInfo.Item2.Page.hasNext;
                            PrevPageBtn.Visible = m_AddressSpaceCategoryPageInfo.Item2.Page.hasPrev;
                        }

                        CategoryExpressions.Clear();
                        FillCriteriaComboBox(typeof(CategorySearchField));
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        private void QueryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            DownloadBtn.Visible = false;
            switch (QueryComboBox.SelectedItem)
            {
                case "AddressSpaces":
                    {
                        DataGridMethods.CreateAddressSpaceView(ResultView);
                        FillCriteriaComboBox(typeof(AddressSpaceSearchField));
                        DownloadBtn.Visible = true;
                        break;
                    }
                case "Categories":
                    {
                        DataGridMethods.CreateCategoriesView(ResultView);
                        FillCriteriaComboBox(typeof(CategorySearchField));
                        break;
                    }
                case "Organisations":
                    {
                        ResultView = DataGridMethods.CreateOrganisationsView(ResultView);
                        FillCriteriaComboBox(typeof(OrganisationSearchField));
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            ResultView.Update();
        }

        private void FillCriteriaComboBox(Type enumType)
        {
            CriteriaComboBox.Items.Clear();
            foreach(string item in Enum.GetNames(enumType))
            {
                CriteriaComboBox.Items.Add(item);
            }
        }

        private async void DownloadBtn_Click(object sender, EventArgs e)
        {
            await ContextQuery();
        }

        private async void ResultView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            await ContextQuery();
        }

        /// <summary>
        /// Either downloads the nodeset or gets the AddressSpaces with the selected category or organisation/contributor
        /// </summary>
        /// <returns></returns>
        private async Task ContextQuery()
        {
            if ((ResultView != null) && (ResultView.CurrentCell != null))
            {
                int currentRow = ResultView.CurrentCell.RowIndex;

                if (QueryComboBox.SelectedItem.ToString() == "AddressSpaces")
                {
                    // Download the selected nodeset
                    string nodesetID = "";
                    nodesetID = m_AddressSpacePageInfo.Item2.Items[currentRow].Item.ID;
                    Tuple<string, AddressSpaceNodeset2> nodeset2 = await client.GetNodeset(nodesetID);
                    if (string.IsNullOrEmpty(nodeset2.Item1))
                    {
                        NodesetResultTextBox.Text = nodeset2.Item2.NodesetXml;
                    }
                    else
                    {
                        NodesetResultTextBox.Text = nodeset2.Item1;
                    }
                    FilePanel.Enabled = true;
                }
                else
                {
                    // Get addressspaces with selected contributor or category
                    AddressSpaceWhereExpression ex = new AddressSpaceWhereExpression();
                    switch (QueryComboBox.SelectedItem)
                    {
                        case "Categories":
                            {
                                ex.Value = m_AddressSpaceCategoryPageInfo.Item2.Items[currentRow].Item.ID;
                                ex.Path = AddressSpaceSearchField.categoryID;
                                break;
                            }
                        case "Organisations":
                            {
                                ex.Value = m_OrganisationPageInfo.Item2.Items[currentRow].Item.ID;
                                ex.Path = AddressSpaceSearchField.contributorID;
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                    m_AddressSpacePageInfo = await client.GetAddressSpaces(pageSize: 10, after: "-1", new List<AddressSpaceWhereExpression> { ex }).ConfigureAwait(false);
                    QueryComboBox.SelectedItem = "AddressSpaces";
                }
            }

            DataGridMethods.FillAddressSpaceView(ResultView, m_AddressSpacePageInfo.Item2);
        }

        private void CrossBtn_Click(object sender, EventArgs e)
        {
            NodesetResultTextBox.Text = "";
            FilePanel.Visible = false;
        }
    }
}
