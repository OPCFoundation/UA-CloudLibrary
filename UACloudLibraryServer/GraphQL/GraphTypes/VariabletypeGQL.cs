using GraphQL.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UA_CloudLibrary.DbContextModels;

namespace UA_CloudLibrary.GraphQL.GraphTypes
{
    public class VariabletypeGQL : EfObjectGraphType<AppDbContext, Variabletype>
    {
        public VariabletypeGQL(IEfGraphQLService<AppDbContext> service) : base(service)
        {
            AutoMap();
        }
    }
}
