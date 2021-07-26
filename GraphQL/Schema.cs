using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace UA_CloudLibrary.GraphQL
{
    public class Schema : global::GraphQL.Types.Schema
    {
        public Schema(IServiceProvider provider) : base(provider)
        {
            // Telling GraphQL what classes to take the fields from
            Query = new CloudLibQuery(provider.GetRequiredService<AppDbContext>());
            //Mutation = new Mutation();
            //Subscription = new Subscription();
        }
    }
}
