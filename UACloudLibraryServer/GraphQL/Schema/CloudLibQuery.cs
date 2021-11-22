using GraphQL;
using GraphQL.EntityFramework;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using UACloudLibrary.Interfaces;

namespace UACloudLibrary
{
    public class CloudLibQuery : QueryGraphType<AppDbContext>
    {
        public CloudLibQuery(IEfGraphQLService<AppDbContext> efGraphQlService, IFileStorage storage, IDatabase database) : base(efGraphQlService)
        {
            AddQueryConnectionField(
                name: "findAddressSpaces",
                arguments: new QueryArguments() {
                    new QueryArgument<StringGraphType>() {
                        Name = "searchtext"
                    }
                },
                resolve: context =>
                {
                    string searchtext = context.GetArgument<string>("searchtext");
                    return context.DbContext.Metadata.Where(
                        p => EF.Functions
                                .ToTsVector("english", p.Metadata_Name + " " + p.Metadata_Value)
                                .Matches(searchtext));
                }
            );
 
            Field<StringGraphType>(
                name: "FindNodeset",
                arguments: new QueryArguments(new QueryArgument(typeof(StringGraphType)) { Name = "keywords" }),
                resolve: context =>
                {
                    return database.FindNodesets(context.GetArgument<string[]>("keywords"));
                });

            Name = "Query";
        }
    }
}