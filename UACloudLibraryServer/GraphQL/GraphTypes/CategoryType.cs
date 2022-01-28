namespace UACloudLibrary
{
    using GraphQL.EntityFramework;
    using GraphQL.Types;
    public class CategoryType : EfObjectGraphType<AppDbContext, AddressSpaceCategory>
    {
        public CategoryType(IEfGraphQLService<AppDbContext> graphQlService) : base(graphQlService)
        {
            Field(e => e.CategoryId);
            Field(e => e.Name);
            AddNavigationField(name: "IconUrl", resolve: e => e.Source.IconUrl, graphType: typeof(UriGraphType));
            Field(e => e.Description);
            Field(e => e.CreationTime);
            Field(e => e.LastModificationTime);
        }
    }
}
