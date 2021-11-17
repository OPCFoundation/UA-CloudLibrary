using System.Collections.Generic;
using System.Windows.Forms;
using UACloudLibClientLibrary;

namespace SampleForm
{
    public static class DataGridMethods
    {
        private static List<DataGridViewColumn> AddressSpaceColumns = new List<DataGridViewColumn>();
        private static List<DataGridViewColumn> OrganisationColumns = new List<DataGridViewColumn>();
        private static List<DataGridViewColumn> CategoryColumns = new List<DataGridViewColumn>();

        static DataGridMethods()
        {
            AddressSpace s = new AddressSpace();
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.IconUrl), HeaderText = "Icon" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.Title), HeaderText = "Title" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.Description), HeaderText = "Description" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.Version), HeaderText = "Version" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.Contributor), HeaderText = "Contributor" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.License), HeaderText = "License" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.Category), HeaderText = "Category" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.PurchasingInformationUrl), HeaderText = "Purchasing Information" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.ReleaseNotesUrl), HeaderText = "Release" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.SupportedLocales), HeaderText = "Supported Locales" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.LastModification), HeaderText = "Last Modification" });
            AddressSpaceColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(s.NumberOfDownloads), HeaderText = "Downloads" });

            Organisation x = new Organisation();
            OrganisationColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(x.LogoUrl), HeaderText = "Logo" });
            OrganisationColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(x.Name), HeaderText = "Name" });
            OrganisationColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(x.Description), HeaderText = "Description" });
            OrganisationColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(x.ContactEmail), HeaderText = "Contact Email" });
            OrganisationColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(x.Website), HeaderText = "Website" });

            AddressSpaceCategory c = new AddressSpaceCategory();
            CategoryColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(c.IconUrl), HeaderText = "Icon" });
            CategoryColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(c.Name), HeaderText = "Name" });
            CategoryColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(c.Description), HeaderText = "Description" });
            CategoryColumns.Add(new DataGridViewTextBoxColumn() { Name = nameof(c.LastModificationTime), HeaderText = "Last Modification" });
        }

        public static DataGridView CreateAddressSpaceView(DataGridView view)
        {
            if ((view != null) && (view.Rows != null) && (view.Columns != null))
            {
                view.Rows.Clear();
                view.Columns.Clear();
                view.Columns.AddRange(AddressSpaceColumns.ToArray());
            }

            return view;
        }

        public static void FillAddressSpaceView(DataGridView view, PageInfo<AddressSpace> pageInfo)
        {
            if ((view != null) && (view.Rows != null) && (view.Columns != null))
            {
                view.Rows.Clear();
                view.Columns.Clear();
                view.Columns.AddRange(AddressSpaceColumns.ToArray());
            }

            if (pageInfo.Items != null)
            {
                foreach (PageItem<AddressSpace> item in pageInfo.Items)
                {
                    int currentRow = view.Rows.Add();
                    view.Rows[currentRow].Cells[nameof(item.Item.IconUrl)].Value = item.Item.IconUrl;
                    view.Rows[currentRow].Cells[nameof(item.Item.Title)].Value = item.Item.Title;
                    view.Rows[currentRow].Cells[nameof(item.Item.Description)].Value = item.Item.Description;
                    view.Rows[currentRow].Cells[nameof(item.Item.Version)].Value = item.Item.Version;
                    view.Rows[currentRow].Cells[nameof(item.Item.Contributor)].Value = item.Item.Contributor.Name;
                    view.Rows[currentRow].Cells[nameof(item.Item.License)].Value = item.Item.License;
                    view.Rows[currentRow].Cells[nameof(item.Item.Category)].Value = item.Item.Category.Name;
                    view.Rows[currentRow].Cells[nameof(item.Item.PurchasingInformationUrl)].Value = item.Item.PurchasingInformationUrl;
                    view.Rows[currentRow].Cells[nameof(item.Item.ReleaseNotesUrl)].Value = item.Item.ReleaseNotesUrl;
                    view.Rows[currentRow].Cells[nameof(item.Item.SupportedLocales)].Value = item.Item.SupportedLocales;
                    view.Rows[currentRow].Cells[nameof(item.Item.LastModification)].Value = item.Item.LastModification;
                    view.Rows[currentRow].Cells[nameof(item.Item.NumberOfDownloads)].Value = item.Item.NumberOfDownloads;
                }
            }
        }

        public static DataGridView CreateOrganisationsView(DataGridView dataGridView)
        {
            if ((dataGridView != null) && (dataGridView.Rows != null) && (dataGridView.Columns != null))
            {
                dataGridView.Rows.Clear();
                dataGridView.Columns.Clear();
                dataGridView.Columns.AddRange(OrganisationColumns.ToArray());
            }

            return dataGridView;
        }

        public static void FillOrganisationView(DataGridView view, PageInfo<Organisation> pageInfo)
        {
            if ((view != null) && (view.Rows != null))
            {
                view.Rows.Clear();
            }

            if ((pageInfo != null) && (pageInfo.Items != null))
            {
                foreach (PageItem<Organisation> item in pageInfo.Items)
                {
                    int currentRow = view.Rows.Add();
                    view.Rows[currentRow].Cells[0].Value = item.Item.LogoUrl;
                    view.Rows[currentRow].Cells[1].Value = item.Item.Name;
                    view.Rows[currentRow].Cells[2].Value = item.Item.Description;
                    view.Rows[currentRow].Cells[3].Value = item.Item.ContactEmail;
                    view.Rows[currentRow].Cells[4].Value = item.Item.Website;
                }
            }
        }

        public static DataGridView CreateCategoriesView(DataGridView view)
        {
            if ((view != null) && (view.Rows != null) && (view.Columns != null))
            {
                view.Rows.Clear();
                view.Columns.Clear();
                view.Columns.AddRange(CategoryColumns.ToArray());
            }

            return view;
        }

        public static void FillCategoriesView(DataGridView view, PageInfo<AddressSpaceCategory> pageInfo)
        {
            if ((view != null) && (view.Rows != null))
            {
                view.Rows.Clear();
            }

            if ((pageInfo != null) && (pageInfo.Items != null))
            {
                foreach (PageItem<AddressSpaceCategory> item in pageInfo.Items)
                {
                    int currentRow = view.Rows.Add();
                    view.Rows[currentRow].Cells[0].Value = item.Item.IconUrl;
                    view.Rows[currentRow].Cells[1].Value = item.Item.Name;
                    view.Rows[currentRow].Cells[2].Value = item.Item.Description;
                    view.Rows[currentRow].Cells[3].Value = item.Item.LastModificationTime;
                }
            }
        }
    }
}
