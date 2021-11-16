using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using UA_CloudLibrary.GraphQL;
using UA_CloudLibrary.GraphQL.GraphTypes;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL
{
    public class Schema : global::GraphQL.Types.Schema
    {
        public Schema(IServiceProvider provider, CloudLibQuery query, Mutations mutation) : base(provider)
        {
            // Defining the schema
            Query = query;
            Mutation = mutation;
            //Subscription = new Subscription();
        }
    }
}
