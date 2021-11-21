using GraphQL.EntityFramework;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class ObjecttypeGQL : EfObjectGraphType<AppDbContext, Objecttype>
    {
        public ObjecttypeGQL(IEfGraphQLService<AppDbContext> service) : base(service)
        {
            AutoMap();
        }
    }
}
