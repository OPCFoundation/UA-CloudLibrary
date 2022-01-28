namespace UACloudLibrary
{
    using GraphQL.EntityFramework;
    using GraphQL.Types;
    public class OrganisationType : EfObjectGraphType<AppDbContext, Organisation>
    {
        public OrganisationType(IEfGraphQLService<AppDbContext> graphQlService) : base(graphQlService)
        {
            Field(e => e.ContributorId);
            Field(e => e.Name);
            Field(e => e.CreationTime);
            Field(e => e.ContactEmail);
            Field(e => e.Description);
            Field(e => e.LastModificationTime);
            AddNavigationField(name: "LogoUrl", resolve: e => e.Source.LogoUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "Website", resolve: e => e.Source.Website, graphType: typeof(UriGraphType));
        }
    }
}
