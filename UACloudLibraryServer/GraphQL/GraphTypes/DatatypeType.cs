
using GraphQL.Types;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class DatatypeType : ObjectGraphType<DatatypeModel>
    {
        public DatatypeType()
        {
            Field(a => a.datatype_id);
            Field(a => a.nodeset_id);
            Field(a => a.datatype_browsename);
            Field(a => a.datatype_value);
            Field(a => a.datatype_namespace);
        }
    }
}
