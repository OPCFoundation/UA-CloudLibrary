
using GraphQL.Types;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class VariabletypeType : ObjectGraphType<VariabletypeModel>
    {
        public VariabletypeType()
        {
            Field(a => a.variabletype_id);
            Field(a => a.nodeset_id);
            Field(a => a.variabletype_browsename);
            Field(a => a.variabletype_value);
            Field(a => a.variabletype_namespace);
        }
    }
}
