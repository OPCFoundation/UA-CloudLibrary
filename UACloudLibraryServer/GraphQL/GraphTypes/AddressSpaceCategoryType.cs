using GraphQL;
using GraphQL.EntityFramework;
using GraphQL.Types;

namespace UACloudLibrary
{
    [GraphQLMetadata(nameof(AddressSpaceCategory))]
    public class AddressSpaceCategoryType : EfObjectGraphType<AppDbContext, AddressSpaceCategory>
    {
        public AddressSpaceCategoryType(IEfGraphQLService<AppDbContext> graphQlService) : base(graphQlService)
        {
            Field<UriGraphType>(Name = "iconUrl", resolve: ctx => ctx.Source.IconUrl);

            AutoMap();
        }
    }
}
