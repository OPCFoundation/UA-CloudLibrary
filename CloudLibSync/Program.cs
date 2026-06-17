using System.CommandLine;
using Microsoft.Extensions.Logging;
using Opc.Ua.CloudLib.Sync;

[assembly: CLSCompliant(false)]
sealed class Program : ILogger
{
    public static Task<int> Main(string[] args)
    {
        return new Program().MainAsync(args);
    }

    public async Task<int> MainAsync(string[] args)
    {
        // Source arguments and options
        var sourceUrlArg = new Argument<string>("sourceUrl") {
            Description = "Source Cloud Library URL"
        };
        var sourceAuthArg = new Argument<string>("sourceAuth") {
            Description = "Source username (for Basic Auth) or API key (for API key auth)"
        };
        var sourcePwdOpt = new Option<string?>("--sourcePassword") {
            Description = "Source password (only required for Basic Auth, omit for API key auth)"
        };

        // Target arguments and options
        var targetUrlArg = new Argument<string>("targetUrl") {
            Description = "Target Cloud Library URL"
        };
        var targetAuthArg = new Argument<string>("targetAuth") {
            Description = "Target username (for Basic Auth) or API key (for API key auth)"
        };
        var targetPwdOpt = new Option<string?>("--targetPassword") {
            Description = "Target password (only required for Basic Auth, omit for API key auth)"
        };

        var overwriteOpt = new Option<bool>("--overwrite") {
            Description = "If specified, allows existing nodesets in a Cloud Library to be overwritten."
        };

        var fileNameOpt = new Option<string>("--fileName") {
            Description = "If specified, uploads only this nodeset file. Otherwise all files in --localDir are uploaded."
        };

        var localDirOpt = new Option<string>("--localDir") {
            Description = "The local directory where to retrieve nodesets to upload.",
            DefaultValueFactory = _ => "Downloads"
        };

        var nodeSetXmlDirOpt = new Option<string>("--nodeSetXmlDir") {
            Description = "If specified the node sets without their metadata (XML only) will be written to this directory."
        };

        var uploadCommand = new Command(
            "upload",
            "Uploads nodesets and their metadata from a local directory to a cloud library.")
        {
            targetUrlArg,
            targetAuthArg,
            targetPwdOpt,
            localDirOpt,
            fileNameOpt,
            overwriteOpt
        };

        var downloadCommand = new Command(
            "download",
            "Downloads all nodesets and their metadata from a Cloud Library to a local directory.")
        {
            sourceUrlArg,
            sourceAuthArg,
            sourcePwdOpt,
            localDirOpt,
            nodeSetXmlDirOpt,
        };

        var syncCommand = new Command(
            "sync",
            "Downloads all nodesets and their metadata from a Cloud Library (source) and uploads it to another Cloud Library (target).")
        {
            sourceUrlArg,
            sourceAuthArg,
            sourcePwdOpt,
            targetUrlArg,
            targetAuthArg,
            targetPwdOpt,
            overwriteOpt
        };

        var root = new RootCommand()
        {
            syncCommand,
            downloadCommand,
            uploadCommand
        };

        uploadCommand.SetAction(parseResult => {
            var targetUrl = parseResult.GetValue(targetUrlArg);
            var targetAuth = parseResult.GetValue(targetAuthArg);
            var targetPassword = parseResult.GetValue(targetPwdOpt);
            var localDir = parseResult.GetValue(localDirOpt) ?? string.Empty;
            var fileName = parseResult.GetValue(fileNameOpt) ?? string.Empty;
            var overwrite = parseResult.GetValue(overwriteOpt);

            if (string.IsNullOrEmpty(targetUrl))
            {
                throw new ArgumentException("targetUrl is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(targetAuth))
            {
                throw new ArgumentException("targetAuth (username or API key) is required.", nameof(args));
            }

            return new CloudLibSync(this).UploadAsync(
                targetUrl,
                targetAuth,
                targetPassword,
                localDir,
                fileName,
                overwrite
            );
        });

        downloadCommand.SetAction(parseResult => {
            var sourceUrl = parseResult.GetValue(sourceUrlArg);
            var sourceAuth = parseResult.GetValue(sourceAuthArg);
            var sourcePassword = parseResult.GetValue(sourcePwdOpt);
            var localDir = parseResult.GetValue(localDirOpt) ?? string.Empty;
            var nodeSetXmlDir = parseResult.GetValue(nodeSetXmlDirOpt) ?? string.Empty;

            if (string.IsNullOrEmpty(sourceUrl))
            {
                throw new ArgumentException("sourceUrl is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(sourceAuth))
            {
                throw new ArgumentException("sourceAuth (username or API key) is required.", nameof(args));
            }

            return new CloudLibSync(this).DownloadAsync(
                sourceUrl,
                sourceAuth,
                sourcePassword,
                localDir,
                nodeSetXmlDir
            );
        });

        syncCommand.SetAction(parseResult => {
            var sourceUrl = parseResult.GetValue(sourceUrlArg);
            var sourceAuth = parseResult.GetValue(sourceAuthArg);
            var sourcePassword = parseResult.GetValue(sourcePwdOpt);
            var targetUrl = parseResult.GetValue(targetUrlArg);
            var targetAuth = parseResult.GetValue(targetAuthArg);
            var targetPassword = parseResult.GetValue(targetPwdOpt);
            var overwrite = parseResult.GetValue(overwriteOpt);

            if (string.IsNullOrEmpty(sourceUrl))
            {
                throw new ArgumentException("sourceUrl is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(sourceAuth))
            {
                throw new ArgumentException("sourceAuth (username or API key) is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(targetUrl))
            {
                throw new ArgumentException("targetUrl is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(targetAuth))
            {
                throw new ArgumentException("targetAuth (username or API key) is required.", nameof(args));
            }

            return new CloudLibSync(this).SynchronizeAsync(
                sourceUrl,
                sourceAuth,
                sourcePassword,
                targetUrl,
                targetAuth,
                targetPassword,
                overwrite
            );
        });

        await root.Parse(args).InvokeAsync().ConfigureAwait(false);

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

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return new MemoryStream();
    }
}
