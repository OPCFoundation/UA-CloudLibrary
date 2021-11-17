using GraphQL;
using GraphQL.EntityFramework;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using UA_CloudLibrary.GraphQL.GraphTypes;
using UACloudLibrary;
using UACloudLibrary.Interfaces;

namespace UA_CloudLibrary.GraphQL
{
    public class CloudLibQuery : QueryGraphType<AppDbContext>
    {
        IFileStorage _storage;
        public CloudLibQuery(IEfGraphQLService<AppDbContext> efGraphQlService) : base(efGraphQlService)
        {
            PostgresSQLDB postgres = new PostgresSQLDB();

            switch (Environment.GetEnvironmentVariable("HostingPlatform"))
            {
                case "Azure": _storage = new AzureFileStorage(); break;
                case "AWS": _storage = new AWSFileStorage(); break;
                case "GCP": _storage = new GCPFileStorage(); break;
#if DEBUG
                default: _storage = new LocalFileStorage(); break;
#else
                default: throw new Exception("Invalid HostingPlatform specified in environment! Valid variables are Azure, AWS and GCP");
#endif
            }


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
                }
                );

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
                    result.Nodeset.NodesetXml = await _storage.DownloadFileAsync(name).ConfigureAwait(false);

                    // TODO: Lookup and add additional metadata
                    return result;
                }
              );

            FieldAsync<StringGraphType>(
                name: "FindNodeset",
                arguments: new QueryArguments(new QueryArgument(typeof(StringGraphType)) { Name = "keywords" }),
                resolve: async context =>
                {
                    return await postgres.FindNodesetsAsync(context.GetArgument<string[]>("keywords"));
                });
            #endregion

            Name = "Query";
        }
    }
}