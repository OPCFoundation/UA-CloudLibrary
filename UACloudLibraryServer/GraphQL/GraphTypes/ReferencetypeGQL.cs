using GraphQL.EntityFramework;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class ReferencetypeGQL : EfObjectGraphType<AppDbContext, Referencetype>
    {
        public ReferencetypeGQL(IEfGraphQLService<AppDbContext> service) : base(service)
        {
            AutoMap();
        }
    }
}
