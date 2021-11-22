using GraphQL.Types;
using System.Collections.Generic;

namespace UACloudLibrary
{
    public class Mutations : ObjectGraphType<object>
    {
        public Mutations(IDatabase database)
        {
            Field<BooleanGraphType>(
                name: "Metadata",
                arguments: new QueryArguments(
                    new QueryArgument<MetadataInput> { Name = "metaTag" },
                    new QueryArgument<IntGraphType> { Name = "nodesetID" }
                    ),
                resolve: context => {
                    uint nodesetID = (uint)context.Arguments["nodesetID"];
                    Dictionary<string, object> metaTag = (Dictionary<string, object>)context.Arguments["metaTag"];
                    return database.AddMetaDataToNodeSet(nodesetID, metaTag["name"].ToString(), metaTag["value"].ToString());
                    }
                );
        }
    }

    public class AddressSpaceInput : InputObjectGraphType<AddressSpace>
    {
        public AddressSpaceInput()
        {
            // Additional Fields as needed
            Field<AddressSpaceNodesetInput>("nodeset", resolve: context => context.Source.Nodeset.NodesetXml);
            Field<StringGraphType>("title", resolve: context => context.Source.Title);
            Field<StringGraphType>("version", resolve: context => context.Source.Version);
            Field<AddressSpaceLicenseType>("license", resolve: context => context.Source.License);
            Field<StringGraphType>("description", resolve: context => context.Source.Description);
        }
    }

    public class AddressSpaceCategoryInput : InputObjectGraphType<AddressSpaceCategory>
    {
        public AddressSpaceCategoryInput()
        {
            Field<StringGraphType>("id", resolve: context => context.Source.Name);
        }
    }

    public class AddressSpaceNodesetInput : InputObjectGraphType<AddressSpaceNodeset2>
    {
        public AddressSpaceNodesetInput()
        {
            Field<StringGraphType>("NodesetXml", resolve: context => context.Source.NodesetXml);
        }
    }

    public class MetadataInput : InputObjectGraphType
    {
        public MetadataInput()
        {
            Field<StringGraphType>("Name");
            Field<StringGraphType>("Value");
        }
    }
}
