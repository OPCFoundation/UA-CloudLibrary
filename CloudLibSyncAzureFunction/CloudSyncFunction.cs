using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opc.Ua.Cloud.Library.Client;
using Opc.Ua.CloudLib.Sync;

[assembly: FunctionsStartup(typeof(CloudLibSyncAzureFunction.CloudSyncFunctionStartup))]

namespace CloudLibSyncAzureFunction
{

    public class CloudLibSyncOptions
    {
        public UACloudLibClient.Options Source { get; set; }
        public UACloudLibClient.Options Target { get; set; }
    }

    public class CloudSyncFunctionStartup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services
                .AddOptions<CloudLibSyncOptions>()
                    .Configure<IConfiguration>((settings, configuration) => {
                        configuration.GetSection("CloudLibrarySync").Bind(settings);
                    });
        }
    }

    public class CloudSyncFunction
    {
        public CloudSyncFunction(IOptions<CloudLibSyncOptions> options)
        {
            if (options == null || options.Value == null)
            {
                throw new ArgumentNullException(nameof(CloudLibSyncOptions));
            }
            if (options.Value.Source == null)
            {
                throw new ArgumentNullException(nameof(options.Value.Source));
            }
            if (options.Value.Target == null)
            {
                throw new ArgumentNullException(nameof(options.Value.Target));
            }
            _options = options.Value;
        }
        readonly CloudLibSyncOptions _options;

        [FunctionName("CloudSyncFunction")]
        public async Task Run([TimerTrigger("0 0 5 * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            await new CloudLibSync(log).SynchronizeAsync(
                _options.Source.EndPoint, _options.Source.Username, _options.Source.Password,
                _options.Target.EndPoint, _options.Target.Username, _options.Target.Password).ConfigureAwait(false);
        }
    }
}

