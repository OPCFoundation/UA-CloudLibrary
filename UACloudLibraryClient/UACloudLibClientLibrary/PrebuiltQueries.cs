using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Query.Builder;

namespace UACloudLibClientLibrary
{
    /// <summary>
    /// Defines which fields are returned
    /// </summary>
    static class PrebuiltQueries
    {
        static IQuery<AddressSpace> _AddressSpaceQuery = new Query<AddressSpace>("node")
            .AddField(h => h.ID)
            .AddField(h => h.Title)
            .AddField(
                h => h.Contributor,
                sq => sq.AddField(h => h.Name)
                        .AddField(h => h.ID)
                        .AddField(h => h.ContactEmail)
                        .AddField(h => h.Website)
                        .AddField(h => h.LogoUrl)
                        .AddField(h => h.Description)
                )
            .AddField(h => h.License)
            .AddField(
                h => h.Category, 
                sq => sq.AddField(h => h.Name)
                        .AddField(h => h.ID)
                        .AddField(h => h.LastModificationTime)
                        .AddField(h => h.IconUrl)
                )
            .AddField(h => h.Description)
            .AddField(h => h.DocumentationUrl)
            .AddField(h => h.PurchasingInformationUrl)
            .AddField(h => h.Version)
            .AddField(h => h.ReleaseNotesUrl)
            .AddField(h => h.KeyWords)
            .AddField(h => h.SupportedLocales)
            .AddField(h => h.LastModification);

        public static string AddressSpaceQuery 
        {
            get { return _AddressSpaceQuery.Build().ToString(); }
        }

        static IQuery<Organisation> _OrgQuery = new Query<Organisation>("node")
            .AddField(f => f.ID)
            .AddField(f => f.Name)
            .AddField(f => f.Website)
            .AddField(f => f.ContactEmail)
            .AddField(f => f.Description)
            .AddField(f => f.LogoUrl);

        public static string OrganisationsQuery { get { return _OrgQuery.Build().ToString(); } }


        static IQuery<AddressSpaceCategory> _CategoryQuery = new Query<AddressSpaceCategory>("node")
            .AddField(f => f.Name)
            .AddField(f => f.ID)
            .AddField(f => f.Description)
            .AddField(f => f.IconUrl)
            .AddField(f => f.LastModificationTime);

        public static string CategoryQuery { get { return _CategoryQuery.Build().ToString(); } }
    }
}
