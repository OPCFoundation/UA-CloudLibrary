using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
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
        var sourceUrlArg = new Argument<string>("sourceUrl");
        var sourceUserArg = new Argument<string>("sourceUserName");
        var sourcePwdArg = new Argument<string>("sourcePassword");
        var targetUrlArg = new Argument<string>("targetUrl");
        var targetUserArg = new Argument<string>("targetUserName");
        var targetPwdArg = new Argument<string>("targetPassword");
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
            targetUserArg,
            targetPwdArg,
            localDirOpt,
            fileNameOpt,
            overwriteOpt
        };

        var downloadCommand = new Command(
            "download",
            "Downloads all nodesets and their metadata from a Cloud Library to a local directory.")
        {
            sourceUrlArg,
            sourceUserArg,
            sourcePwdArg,
            localDirOpt,
            nodeSetXmlDirOpt,
        };

        var syncCommand = new Command(
            "sync",
            "Downloads all nodesets and their metadata from a Cloud Library (source) and uploads it to another Cloud Library (target).")
        {
            sourceUrlArg,
            sourceUserArg,
            sourcePwdArg,
            targetUrlArg,
            targetUserArg,
            targetPwdArg,
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
            var targetUserName = parseResult.GetValue(targetUserArg);
            var targetPassword = parseResult.GetValue(targetPwdArg);
            var localDir = parseResult.GetValue(localDirOpt) ?? string.Empty;
            var fileName = parseResult.GetValue(fileNameOpt) ?? string.Empty;
            var overwrite = parseResult.GetValue(overwriteOpt);

            if (string.IsNullOrEmpty(targetUrl))
            {
                throw new ArgumentException("targetUrl is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(targetUserName))
            {
                throw new ArgumentException("targetUserName is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(targetPassword))
            {
                throw new ArgumentException("targetPassword is required.", nameof(args));
            }

            return new CloudLibSync(this).UploadAsync(
                targetUrl,
                targetUserName,
                targetPassword,
                localDir,
                fileName,
                overwrite
            );
        });

        downloadCommand.SetAction(parseResult => {
            var sourceUrl = parseResult.GetValue(sourceUrlArg);
            var sourceUserName = parseResult.GetValue(sourceUserArg);
            var sourcePassword = parseResult.GetValue(sourcePwdArg);
            var localDir = parseResult.GetValue(localDirOpt) ?? string.Empty;
            var nodeSetXmlDir = parseResult.GetValue(nodeSetXmlDirOpt) ?? string.Empty;

            if (string.IsNullOrEmpty(sourceUrl))
            {
                throw new ArgumentException("sourceUrl is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(sourceUserName))
            {
                throw new ArgumentException("sourceUserName is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(sourcePassword))
            {
                throw new ArgumentException("sourcePassword is required.", nameof(args));
            }

            return new CloudLibSync(this).DownloadAsync(
                sourceUrl,
                sourceUserName,
                sourcePassword,
                localDir,
                nodeSetXmlDir
            );
        });

        syncCommand.SetAction(parseResult => {
            var sourceUrl = parseResult.GetValue(sourceUrlArg);
            var sourceUserName = parseResult.GetValue(sourceUserArg);
            var sourcePassword = parseResult.GetValue(sourcePwdArg);
            var targetUrl = parseResult.GetValue(targetUrlArg);
            var targetUserName = parseResult.GetValue(targetUserArg);
            var targetPassword = parseResult.GetValue(targetPwdArg);
            var overwrite = parseResult.GetValue(overwriteOpt);

            if (string.IsNullOrEmpty(sourceUrl))
            {
                throw new ArgumentException("sourceUrl is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(sourceUserName))
            {
                throw new ArgumentException("sourceUserName is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(sourcePassword))
            {
                throw new ArgumentException("sourcePassword is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(targetUrl))
            {
                throw new ArgumentException("targetUrl is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(targetUserName))
            {
                throw new ArgumentException("targetUserName is required.", nameof(args));
            }
            if (string.IsNullOrEmpty(targetPassword))
            {
                throw new ArgumentException("targetPassword is required.", nameof(args));
            }

            return new CloudLibSync(this).SynchronizeAsync(
                sourceUrl,
                sourceUserName,
                sourcePassword,
                targetUrl,
                targetUserName,
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
