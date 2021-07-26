using GraphQL.Types;
using System.Collections.Generic;
using System.Threading;
using UA_CloudLibrary.GraphQL.Types;
using GraphQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL
{
    public class CloudLibQuery : ObjectGraphType
    {
        public CloudLibQuery(AppDbContext dbContext)
        {
            // Setting up the queries using their specific GraphQL type
            Field<ListGraphType<AddressSpaceType>>()
                .Name("AddressSpaces")
                .Description("Finds all AddressSpaces in the DB")
                .ResolveAsync(async context => {
                        return await dbContext.AddressSpaces.ToListAsync(CancellationToken.None);
                    });

            Field<ListGraphType<AddressSpaceCategoryType>>()
                .Name("AddressSpaceCategories")
                .Description("Finds all AddressSpaceCategories in the DB")
                .ResolveAsync(async context => {
                    return await dbContext.AddressSpaceCategories.ToListAsync(CancellationToken.None);
                });

            Field<ListGraphType<AddressSpaceNodeset2Type>>()
                .Name("AddressSpacesNodesets")
                .Description("Finds all Nodesets in the DB")
                .ResolveAsync(async context => {
                    return await dbContext.AddressSpaceNodesets.ToListAsync(CancellationToken.None);
                });

            Field<ListGraphType<OrganisationType>>()
                .Name("Organisations")
                .Description("Finds all Organisations in the DB")
                .ResolveAsync(async context => {
                    return await dbContext.Organisations.ToListAsync(CancellationToken.None);
                });

            Field<OrganisationType>()
                .Name("Organisation")
                .Description("Finds a single Organisation by ID")
                .Argument<StringGraphType>("ID")
                .ResolveAsync(async context => {
                     string arg = context.GetArgument<string>("ID");
                     return await dbContext.Organisations.FindAsync(arg);
                });

            Field<AddressSpaceCategoryType>()
                .Name("AddressSpaceCategory")
                .Description("Gets Category with matching parameter")
                .Argument<StringGraphType>("ID")
                .ResolveAsync(async context => {
                    string arg = context.GetArgument<string>("ID");
                    return await dbContext.AddressSpaceCategories.FindAsync(arg);
                });

            Field<AddressSpaceType>()
                .Name("AddressSpace")
                .Description("")
                .Argument<StringGraphType>("ID")
                .ResolveAsync(async context => {
                    string arg = context.GetArgument<string>("ID");
                    return await dbContext.AddressSpaces.FindAsync(arg);
                });

            Name = "Query";
            Description = "All the Queries";
        }
    }
}