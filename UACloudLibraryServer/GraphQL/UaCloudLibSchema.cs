using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace UACloudLibrary
{
    public class UaCloudLibSchema : Schema
    {
        public UaCloudLibSchema(IServiceProvider provider)
            : base(provider)
        {
            Query = provider.GetRequiredService<UaCloudLibQuery>();
        }
    }
}
