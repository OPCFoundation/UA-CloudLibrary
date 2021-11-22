
using GraphQL.Types;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class ReferencetypeType : ObjectGraphType<ReferencetypeModel>
    {
        public ReferencetypeType()
        {
            Field(a => a.referencetype_id);
            Field(a => a.nodeset_id);
            Field(a => a.referencetype_browsename);
            Field(a => a.referencetype_value);
            Field(a => a.referencetype_namespace);
        }
    }
}
