using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UACloudLibClientLibrary;

namespace SampleForm
{
    public static class DataGridMethods
    {
        private static List<DataGridViewColumn> AddressSpaceColumns = new List<DataGridViewColumn>();
        private static List<DataGridViewColumn> OrganisationColumns = new List<DataGridViewColumn>();
        private static List<DataGridViewColumn> CategoryColumns = new List<DataGridViewColumn>();
        private static List<DataGridViewColumn> CombinedView = new List<DataGridViewColumn>();
        private static Dictionary<int, long> RowNodeset = new Dictionary<int, long>();
            

        public static long GetNodesetId(int selectedIndex)
        {
            return RowNodeset[selectedIndex];
        }

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

            CombinatedTypes types = new CombinatedTypes();
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.AddressSpace.Title), HeaderText = "Title" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.AddressSpace.Version), HeaderText = "Version" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.Organisation.Name), HeaderText = "Contributor" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.Organisation.ContactEmail), HeaderText = "Contact" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.AddressSpace.SupportedLocales), HeaderText = "Supported Locales" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.Nodeset.LastModification), HeaderText = "Last Modification" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.AddressSpace.License), HeaderText = "License" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.AddressSpace.LicenseUrl), HeaderText = "License Information" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.AddressSpace.PurchasingInformationUrl), HeaderText = "Purchasing Information" });
            CombinedView.Add(new DataGridViewTextBoxColumn() { Name = nameof(types.AddressSpace.NumberOfDownloads), HeaderText = "Downloads" });
        }

        public static DataGridView CreateCombinedView(DataGridView view)
        {
            view.Rows.Clear();
            view.Columns.Clear();
            view.Columns.AddRange(CombinedView.ToArray());
            return view;
        }

        public static void FillCombinedView(DataGridView view, List<CombinatedTypes> types)
        {
            view.Rows.Clear();
            //CreateCombinedView(view);

            RowNodeset.Clear();

            foreach (CombinatedTypes type in types)
            {
                string formattedText = "";
                for(int i = 0; i < type.AddressSpace.SupportedLocales.Length; i++)
                {
                    formattedText += type.AddressSpace.SupportedLocales[i];
                    if(i+1 != type.AddressSpace.SupportedLocales.Length)
                    {
                        formattedText += ", ";
                    }
                }
                int currentRow = view.Rows.Add();
                view.Rows[currentRow].Cells[nameof(type.AddressSpace.Title)].Value = type.AddressSpace.Title;
                view.Rows[currentRow].Cells[nameof(type.AddressSpace.Version)].Value = type.AddressSpace.Version;
                view.Rows[currentRow].Cells[nameof(type.AddressSpace.SupportedLocales)].Value = formattedText;
                view.Rows[currentRow].Cells[nameof(type.AddressSpace.LastModification)].Value = type.Nodeset.LastModification;
                view.Rows[currentRow].Cells[nameof(type.AddressSpace.NumberOfDownloads)].Value = type.AddressSpace.NumberOfDownloads;
                view.Rows[currentRow].Cells[nameof(type.AddressSpace.LicenseUrl)].Value = type.AddressSpace.LicenseUrl;
                view.Rows[currentRow].Cells[nameof(type.AddressSpace.License)].Value = type.AddressSpace.License;
                view.Rows[currentRow].Cells[nameof(type.AddressSpace.PurchasingInformationUrl)].Value = type.AddressSpace.PurchasingInformationUrl;
                view.Rows[currentRow].Cells[nameof(type.Organisation.Name)].Value = type.Organisation.Name;
                view.Rows[currentRow].Cells[nameof(type.Organisation.ContactEmail)].Value = type.Organisation.ContactEmail;

                RowNodeset[currentRow] = type.NodesetID;
            }

            view.AutoResizeColumns();
        }
    }
}
