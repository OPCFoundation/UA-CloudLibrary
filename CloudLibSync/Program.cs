using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.Logging;
using Opc.Ua.CloudLib.Sync;

class Program : ILogger
{
    public static Task<int> Main(string[] args)
    {
        return new Program().MainAsync(args);
    }

    public async Task<int> MainAsync(string[] args)
    {
        var downloadCommand = new Command("download", "Downloads all nodesets and their metadata from a Cloud Library to a local directory.")
        {
              new Argument<string>("sourceUrl") {},
              new Argument<string>("sourceUserName") {},
              new Argument<string>("sourcePassword") {},
              new Option<string>("--localDir", () => "Downloads") { },
              new Option<string>("--nodeSetXmlDir", "If specified the node sets without their metadata (XML only) will be written to this directory.") { },
        };
        downloadCommand.Handler = CommandHandler.Create(new CloudLibSync(this).DownloadAsync);

        var syncCommand = new Command("sync", "Downloads all nodests and their metadata from a Cloud Library (source) and uploads it to another Cloud Library (target).")
        {
              new Argument<string>("sourceUrl") {},
              new Argument<string>("sourceUserName") {},
              new Argument<string>("sourcePassword") { },
              new Argument<string>("targetUrl") { },
              new Argument<string>("targetUserName") {},
              new Argument<string>("targetPassword") {},
        };
        syncCommand.Handler = CommandHandler.Create(new CloudLibSync(this).SynchronizeAsync);


        var uploadCommand = new Command("upload", "Uploads nodesets and their metadata from a local directory to a cloud library.")
            {
              new Argument<string>("targetUrl") {},
              new Argument<string>("targetUserName") {},
              new Argument<string>("targetPassword") {},
              new Option<string>("--localDir", () => "Downloads") {},
              new Option<string>("--fileName", "If specified, uploads only this nodeset file. Otherwise all files in --localDir are uploaded.") {},
            };
        uploadCommand.Handler = CommandHandler.Create(new CloudLibSync(this).UploadAsync);

        var root = new RootCommand()
        {
            syncCommand,
            downloadCommand,
            uploadCommand,
        };

        await root.InvokeAsync(args).ConfigureAwait(false);

        return 0;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new MemoryStream();
    }
}
