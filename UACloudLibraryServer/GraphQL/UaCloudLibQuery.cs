using GraphQL.Types;

namespace UACloudLibrary
{
    public class UaCloudLibQuery : ObjectGraphType
    {
        public UaCloudLibQuery(UaCloudLibRepo cloudLibRepo)
        {
            Name = "UACloudLibraryQuery";

            Field<ListGraphType<DatatypeType>>(
                "datatype",
                resolve: context => cloudLibRepo.GetDataTypes()
            );

            Field<ListGraphType<MetadataType>>(
                "metadata",
                resolve: context => cloudLibRepo.GetMetaData()
            );

            Field<ListGraphType<ObjecttypeType>>(
                "objectype",
                resolve: context => cloudLibRepo.GetObjectTypes()
            );

            Field<ListGraphType<ReferencetypeType>>(
                "referencetype",
                resolve: context => cloudLibRepo.GetReferenceTypes()
            );

            Field<ListGraphType<VariabletypeType>>(
                "variabletype",
                resolve: context => cloudLibRepo.GetVariableTypes()
            );
        }
    }
}
