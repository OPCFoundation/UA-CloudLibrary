using GraphQL.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UA_CloudLibrary.DbContextModels;

namespace UA_CloudLibrary.GraphQL.GraphTypes
{
    public class ObjecttypeGQL : EfObjectGraphType<AppDbContext, Objecttype>
    {
        public ObjecttypeGQL(IEfGraphQLService<AppDbContext> service) : base(service)
        {
            AutoMap();
        }
    }
}
