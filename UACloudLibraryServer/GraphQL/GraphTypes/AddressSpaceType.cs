namespace UACloudLibrary
{
    using GraphQL.EntityFramework;
    using GraphQL.Types;
    using System.Linq;

    public class AddressSpaceType : EfObjectGraphType<AppDbContext, AddressSpaceModel>
    {
        public AddressSpaceType(IEfGraphQLService<AppDbContext> graphQlService) : base(graphQlService)
        {
            Field(e => e.Title);
            Field(e => e.Version);
            Field(e => e.Description);
            Field(e => e.CopyrightText);
            Field(e => e.NumberOfDownloads);
            Field(e => e.CreationTime);
            Field(e => e.LastModificationTime);
            Field(e => e.SupportedLocales);
            Field(e => e.Keywords);
            Field(e => e.License);
            Field(e => e.NodesetId);
            AddNavigationField(name: "Contributor", resolve: e => e.DbContext.organisation.Find(e.Source.ContributorId));
            AddNavigationField(name: "Category", resolve: e => e.DbContext.category.Find(e.Source.CategoryId));
            AddNavigationField(name: "LicenseUrl", resolve: e => e.Source.LicenseUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "DocumentationUrl", resolve: e => e.Source.DocumentationUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "IconUrl", resolve: e => e.Source.IconUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "purchasinginformationUrl", resolve: e => e.Source.PurchasingInformationUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "releaseNotesUrl", resolve: e => e.Source.ReleaseNotesUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "testSpecificationUrl", resolve: e => e.Source.TestSpecificationUrl, graphType: typeof(UriGraphType));
            //Field<LongGraphType, long>("nodesetid");
        }
    }
}
