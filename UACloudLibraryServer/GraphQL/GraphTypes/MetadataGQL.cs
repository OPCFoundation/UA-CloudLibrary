using GraphQL.EntityFramework;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class MetadataGQL : EfObjectGraphType<AppDbContext, Metadata>
    {
        public MetadataGQL(IEfGraphQLService<AppDbContext> service) : base(service)
        {
            AutoMap();
        }
    }
}
