using System;

namespace UACloudLibrary
{
    public class Schema : global::GraphQL.Types.Schema
    {
        public Schema(IServiceProvider provider, CloudLibQuery query, Mutations mutation) : base(provider)
        {
            // Defining the schema
            Query = query;
            Mutation = mutation;
        }
    }
}
