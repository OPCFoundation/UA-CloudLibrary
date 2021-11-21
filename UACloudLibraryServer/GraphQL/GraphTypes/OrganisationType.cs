using GraphQL;
using GraphQL.EntityFramework;
using GraphQL.Types;

namespace UACloudLibrary
{
    [GraphQLMetadata(nameof(Organisation))]
    public class OrganisationType : EfObjectGraphType<AppDbContext, Organisation>
    {
        public OrganisationType(IEfGraphQLService<AppDbContext> graphQlService) : base(graphQlService)
        {
            // Specifying the fields with uncommon types
            Field<UriGraphType>(Name = "logoUrl", resolve: ctx => ctx.Source.LogoUrl);
            Field<UriGraphType>(Name = "website", resolve: ctx => ctx.Source.Website);

            AutoMap();
        }
    }
}
