using GraphQL.Utilities;
using System;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class Schema : GraphQL.Types.Schema
    {
        public Schema(IServiceProvider provider, CloudLibQuery query, Mutations mutation) : base(provider)
        {
            GraphTypeTypeRegistry.Register<Datatype, DatatypeGQL>();
            GraphTypeTypeRegistry.Register<Metadata, MetadataGQL>();
            GraphTypeTypeRegistry.Register<Objecttype, ObjecttypeGQL>();
            GraphTypeTypeRegistry.Register<Referencetype, ReferencetypeGQL>();
            GraphTypeTypeRegistry.Register<Variabletype, VariabletypeGQL>();

            GraphTypeTypeRegistry.Register<AddressSpaceLicense, AddressSpaceLicenseType>();

            Query = query;
            Mutation = mutation;
        }
    }
}
