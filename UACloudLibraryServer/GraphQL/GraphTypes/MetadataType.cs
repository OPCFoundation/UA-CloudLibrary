using GraphQL.Types;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class MetadataType : ObjectGraphType<MetadataModel>
    {
        public MetadataType()
        {
            Field(a => a.metadata_id);
            Field(a => a.nodeset_id);
            Field(a => a.metadata_name);
            Field(a => a.metadata_value);
        }
    }
}
