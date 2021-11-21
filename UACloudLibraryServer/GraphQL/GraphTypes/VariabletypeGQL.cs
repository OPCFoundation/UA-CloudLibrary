using GraphQL.EntityFramework;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class VariabletypeGQL : EfObjectGraphType<AppDbContext, Variabletype>
    {
        public VariabletypeGQL(IEfGraphQLService<AppDbContext> service) : base(service)
        {
            AutoMap();
        }
    }
}
