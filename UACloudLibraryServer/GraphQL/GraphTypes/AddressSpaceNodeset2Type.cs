using GraphQL;
using GraphQL.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL.GraphTypes
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
