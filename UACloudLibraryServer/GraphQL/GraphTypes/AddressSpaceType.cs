using GraphQL;
using GraphQL.EntityFramework;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL.GraphTypes
{
    [GraphQLMetadata(nameof(AddressSpace))]
    public class AddressSpaceType : EfObjectGraphType<AppDbContext, AddressSpace>
    {
        public AddressSpaceType(IEfGraphQLService<AppDbContext> graphQLService) : base(graphQLService)
        {
            // Defining the types that it doesnt find automatically
            Field<UriGraphType>(Name = "documentationUrl", resolve: ctx => ctx.Source.DocumentationUrl);
            Field<UriGraphType>(Name = "iconUrl", resolve: ctx => ctx.Source.IconUrl);
            Field<UriGraphType>(Name = "purchasingInformationUrl", resolve: ctx => ctx.Source.PurchasingInformationUrl);
            Field<UriGraphType>(Name = "licenseUrl", resolve: ctx => ctx.Source.LicenseUrl);
            Field<UriGraphType>(Name = "testSpecificationUrl", resolve: ctx => ctx.Source.TestSpecificationUrl);
            Field<UriGraphType>(Name = "releaseNotesUrl", resolve: ctx => ctx.Source.ReleaseNotesUrl);
            
            Field<ListGraphType<StringGraphType>>(Name = "supportedLocales", resolve: ctx => ctx.Source.SupportedLocales);
            Field<ListGraphType<StringGraphType>>(Name = "keywords", resolve: ctx => ctx.Source.Keywords);

            Field<AddressSpaceLicenseType>(Name = "license", resolve: ctx => ctx.Source.License);

            // Declaring the subquery
            AddNavigationField<Organisation>(Name = "contributor", resolve: ctx => ctx.DbContext.Organisations.Find(ctx.Source.ContributorID));
            AddNavigationField<AddressSpaceCategory>(Name = "category", resolve: ctx => ctx.DbContext.AddressSpaceCategories.Find(ctx.Source.CategoryID));

            AutoMap(
                // Defining the properties that can be ignored
                new List<string>() {
                    "Nodeset",
                    "CategoryID",
                    "ContributorID",
                    "AdditionalProperties"
                });
        }
    }
}
