using GraphQL;
using GraphQL.EntityFramework;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL.GraphTypes
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
