using GraphQL;
using GraphQL.EntityFramework;

namespace UACloudLibrary
{
    [GraphQLMetadata(nameof(AddressSpaceNodeset2))]
    public class AddressSpaceNodeset2Type : EfObjectGraphType<AppDbContext, AddressSpaceNodeset2>
    {
        public AddressSpaceNodeset2Type(IEfGraphQLService<AppDbContext> graphQlService) : base(graphQlService)
        {
            AutoMap();
        }
    }
}
