using GraphQL.EntityFramework;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UA_CloudLibrary.DbContextModels;

namespace UA_CloudLibrary.GraphQL.GraphTypes
{
    public class DatatypeGQL : EfObjectGraphType<AppDbContext, Datatype>
    {
        public DatatypeGQL(IEfGraphQLService<AppDbContext> service) : base(service)
        {
            //Field<IntGraphType>("NodesetID", resolve: context => context.Source.NodesetId);
            AutoMap();
        }
    }
}
