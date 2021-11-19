using GraphQL;
using GraphQL.EntityFramework;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using UA_CloudLibrary.GraphQL.GraphTypes;
using UACloudLibrary;
using UACloudLibrary.Interfaces;

namespace UA_CloudLibrary.GraphQL
{
    public class CloudLibQuery : QueryGraphType<AppDbContext>
    {
        public CloudLibQuery(IEfGraphQLService<AppDbContext> efGraphQlService, IFileStorage storage, IDatabase database) : base(efGraphQlService)
        {
            AddQueryConnectionField(
                name: "addressSpaces",
                resolve: context => context.DbContext.AddressSpaces
                );

            AddSingleField(
                name: "addressSpace",
                resolve: context => context.DbContext.AddressSpaces
                );

            AddQueryConnectionField(
                name: "organisations",
                resolve: context => context.DbContext.Organisations
                );

            AddSingleField(
                name: "organisation",
                resolve: context => context.DbContext.Organisations
                );

            AddQueryConnectionField(
                name: "addressSpaceCategories",
                resolve: context => context.DbContext.AddressSpaceCategories
                );

            AddSingleField(
                name: "addressSpaceCategory",
                resolve: context => context.DbContext.AddressSpaceCategories
                );

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
                    return context.DbContext.AddressSpaces.Where(
                                p => EF.Functions
                                        .ToTsVector("english", p.Title + " " + p.Description)
                                        .Matches(searchtext));
                });

            AddSingleField(
                name: "Nodeset",
                resolve: context =>
                {
                    return context.DbContext.AddressSpaceNodesets;
                });

            #region Specification Queries

            FieldAsync<AddressSpaceType>(
                name: "downloadNodesetFromStorage",
                arguments: new QueryArguments(new QueryArgument(typeof(StringGraphType)) { Name = "name" }),
                resolve: async context =>
                {
                    string name = context.GetArgument<string>("name");
                    AddressSpace result = new AddressSpace();
                    result.Nodeset.NodesetXml = await storage.DownloadFileAsync(name).ConfigureAwait(false);

                    // TODO: Lookup and add additional metadata
                    return result;
                }
              );

            Field<StringGraphType>(
                name: "FindNodeset",
                arguments: new QueryArguments(new QueryArgument(typeof(StringGraphType)) { Name = "keywords" }),
                resolve: context =>
                {
                    return database.FindNodesets(context.GetArgument<string[]>("keywords"));
                });
            #endregion

            Name = "Query";
        }
    }
}