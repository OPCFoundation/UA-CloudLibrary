
using GraphQL.Types;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class ObjecttypeType : ObjectGraphType<ObjecttypeModel>
    {
        public ObjecttypeType()
        {
            Field(a => a.objecttype_id);
            Field(a => a.nodeset_id);
            Field(a => a.objecttype_browsename);
            Field(a => a.objecttype_value);
            Field(a => a.objecttype_namespace);
        }
    }
}
