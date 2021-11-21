using GraphQL.EntityFramework;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class DatatypeGQL : EfObjectGraphType<AppDbContext, Datatype>
    {
        public DatatypeGQL(IEfGraphQLService<AppDbContext> service) : base(service)
        {
            AutoMap();
        }
    }
}
