using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL.Types
{
    public class AddressSpaceType : ObjectGraphType<AddressSpace>
    {
        public AddressSpaceType()
        {
            Name = "AddressSpace";
            Description = "A fitting description awaits";

            Field(x => x.ID);
            Field(x => x.Title);
            Field(x => x.Version);
            Field<StringGraphType>("Licence" , resolve: context => context.Source.License.ToString());
            Field(x => x.CopyrightText);
            Field(x => x.CreationTimeStamp);
            Field(x => x.LastModification);
            Field(x => x.DocumentationUrl, nullable: true);
            Field(x => x.IconUrl, nullable: true);
            Field(x => x.LicenseUrl, nullable: true);
            Field(x => x.Keywords);
            Field(x => x.PurchasingInformationUrl, nullable: true);
            Field(x => x.ReleaseNotesUrl, nullable: true);
            Field(x => x.TestSpecificationUrl, nullable: true);
            Field(x => x.SupportedLocales, nullable: true);
            Field(x => x.NumberOfDownloads, nullable: true);
            Field(x => x.Description);

            Field<OrganisationType>()
                .Name("contributor")
                .ResolveAsync(async context => 
                {
                    Organisation result;
                    // Implemented like this to avoid simultanious execution of dbcontext
                    using (AppDbContext dbContext = AppDbContextFactory.CreateDbContext(null))
                    {                        
                        result = await dbContext.Organisations.FindAsync(context.Source.ContributorID);
                    }
                    return result;
                });

            Field<AddressSpaceCategoryType>()
                .Name("category")
                .ResolveAsync(async context =>
                {
                    AddressSpaceCategory result;
                    using (AppDbContext dbContext = AppDbContextFactory.CreateDbContext(null))
                    {                        
                        result = await dbContext.AddressSpaceCategories.FindAsync(context.Source.CategoryID);
                    }
                    return result;
                });

            Field<AddressSpaceNodeset2Type>()
                .Name("nodeset")
                .ResolveAsync(async context =>
                {
                    AddressSpaceNodeset2 result;
                    using (AppDbContext dbContext = AppDbContextFactory.CreateDbContext(null))
                    {
                        result = await dbContext.AddressSpaceNodesets.FindAsync(context.Source.ID);
                    }
                    return result;
                });

            //Field(x => x.AdditionalProperties);
        }
    }
}
