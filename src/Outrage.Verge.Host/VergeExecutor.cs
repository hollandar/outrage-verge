
using Compose.Path;
using Microsoft.Extensions.DependencyInjection;
using Outrage.Verge.Processor;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Components.Forms;
using System.Runtime.CompilerServices;
using System.CommandLine;
using Outrage.Verge.Processor.Html;
using Outrage.Verge.Processor.Markdown;
using Outrage.Verge.Processor.Interceptors;
using Outrage.Verge.Processor.Generators;
using System.Linq.Expressions;
using Compose.Serialize;
using Outrage.Verge.Configuration;
using GlobExpressions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Outrage.Verge.Build;

namespace Outrage.Verge.Host;

public class VergeExecutor : IDisposable
{
    private readonly PathBuilder inputPath;
    private readonly PathBuilder outputPath;
    private readonly IServiceProvider serviceProvider;
    private bool exiting = false;
    private int rebuildRequests = 0;
    private bool disposedValue;
    private FileSystemWatcher? watcher;
    private Thread? rebuildThread;
    private bool initial = true;

    protected VergeExecutor(PathBuilder inputPath, PathBuilder outputPath, IServiceProvider serviceProvider)
    {
        this.inputPath = inputPath;
        this.outputPath = outputPath;
        this.serviceProvider = serviceProvider;
    }

    protected VergeExecutor(PathBuilder inputPath, PathBuilder outputPath, IServiceCollection? services = null, LogLevel logLevel = LogLevel.Information)
    {
        this.inputPath = inputPath;
        this.outputPath = outputPath;
        this.serviceProvider = BuildServiceProvider(services, logLevel);
    }

    protected void StopWatching()
    {
        if (this.watcher != null)
            this.watcher.EnableRaisingEvents = false;
    }

    protected void StartWatching()
    {
        if (this.watcher == null)
        {
            this.watcher = new FileSystemWatcher(inputPath);
            this.watcher.Changed += Watcher_Changed;
            this.watcher.Created += Watcher_Changed;
            this.watcher.Deleted += Watcher_Changed;
            watcher.IncludeSubdirectories = true;
        }

        if (rebuildThread == null)
        {
            rebuildThread = new Thread(new ThreadStart(RebuildStart));
            rebuildThread.IsBackground = true;
            rebuildThread.Start();
        }

        this.watcher.EnableRaisingEvents = true;
    }

    protected IServiceProvider BuildServiceProvider(IServiceCollection? services, LogLevel logLevel = LogLevel.Information)
    {
        if (services == null) services = new ServiceCollection();
        services.AddLogging(options =>
        {
            options.AddConsole().SetMinimumLevel(logLevel);
        });
        services.AddSingleton<IInterceptor, IncludeInterceptor>();
        services.AddSingleton<IInterceptor, DefineInterceptor>();
        services.AddSingleton<IInterceptor, JsonInterceptor>();
        services.AddSingleton<IInterceptor, ForEachInterceptor>();
        services.AddSingleton<IInterceptor, CodeInterceptor>();
        services.AddSingleton<IInterceptor, PictureInterceptor>();
        services.AddSingleton<IInterceptor, ComponentInterceptor>();
        services.AddSingleton<IInterceptor, RequireInterceptor>();
        services.AddSingleton<IInterceptor, MarkdownInterceptor>();
        services.AddSingleton<IInterceptor, OnLinkInterceptor>();
        services.AddSingleton<IInterceptor, OptionalInterceptor>();
        services.AddSingleton<IContentGenerator, SitemapGenerator>();
        services.AddSingleton<IProcessorFactory, HtmlProcessorFactory>();
        services.AddSingleton<IProcessorFactory, MarkdownProcessorFactory>();

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider;
    }

    public static async Task Start(string[] args, IServiceCollection? services = null)
    {
        var rootCommand = new RootCommand("Verge site generation command line.");

        var serveCommand = new Command("serve", "Rebuild the site automatically and serve it via Kestrel.");
        rootCommand.AddCommand(serveCommand);
        var buildCommand = new Command("build", "Build the site.");
        rootCommand.AddCommand(buildCommand);
        var inputPathOption = new Option<string>("--in", "The path to the input site configuration.");
        rootCommand.AddGlobalOption(inputPathOption);
        var outputPathOption = new Option<string>("--out", "The path to write the published site to.");
        rootCommand.AddGlobalOption(outputPathOption);
        var logLevelOption = new Option<LogLevel>("--loglevel", () => LogLevel.Information, "The minimum log level to present. Trace, Debug, Information, Warning, Error.");
        rootCommand.AddGlobalOption(logLevelOption);

        serveCommand.SetHandler(async (inputPathValue, outputPathValue, logLevelValue) =>
        {
            var inputPath = PathBuilder.From(inputPathValue).CombineIfRelative();
            var outputPath = PathBuilder.From(outputPathValue).CombineIfRelative();

            using (var executor = new VergeExecutor(inputPath, outputPath, services, logLevelValue))
            {
                await executor.RebuildSite();

                if (true)
                {
                    executor.StartWatching();
                    executor.Host(args, services);
                }
            }
        }, inputPathOption, outputPathOption, logLevelOption);

        buildCommand.SetHandler(async (inputPathValue, outputPathValue, logLevelValue) =>
        {
            var inputPath = PathBuilder.From(inputPathValue).CombineIfRelative();
            var outputPath = PathBuilder.From(outputPathValue).CombineIfRelative();

            using (var executor = new VergeExecutor(inputPath, outputPath, services, logLevelValue))
            {
                await executor.RebuildSite();
            }
        }, inputPathOption, outputPathOption, logLevelOption);

        await rootCommand.InvokeAsync(args);
    }

    protected void Host(string[] args, IServiceCollection? services)
    {
        var builder = WebApplication.CreateBuilder(args);
        if (services != null) foreach (var serviceDescriptor in services)
            {
                builder.Services.Add(serviceDescriptor);
            }

        var app = builder.Build();

        app.UseHttpsRedirection();
        var fileProvider = new PhysicalFileProvider(outputPath);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            FileProvider = fileProvider,
            DefaultContentType = "text/plain"
        });

        app.Run();
    }

    protected void Rebuild()
    {
        rebuildRequests++;
    }

    protected void RebuildStart()
    {
        do
        {
            if (rebuildRequests > 0)
            {
                Thread.Sleep(500);
                rebuildRequests = 0;

                var rebuildTask = RebuildSite();
                Task.WaitAll(rebuildTask);
            }
            else
            {
                Thread.Sleep(500);
            }
        }
        while (!exiting);
    }

    public async Task RebuildSite()
    {
        using var scope = this.serviceProvider.CreateScope();

        var siteProcessor = new BuildProcessor(inputPath, serviceProvider, initial);
        initial = false;

        var logger = scope.ServiceProvider.GetService<ILogger<VergeExecutor>>();
        Action<string, LogLevel> log = (msg, level) =>
        {
            if (logger != null)
            {
                logger.Log(level, msg);
            }
            else
            {
                Console.WriteLine($"{level.ToString()}: {msg}");
            }
        };

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        try
        {
            await siteProcessor.Process();
        }
        catch (Exception e)
        {
            log(e.Message, LogLevel.Error);
        }
        stopwatch.Stop();

        log($"Processing took: {stopwatch.ElapsedMilliseconds}.", LogLevel.Information);
    }

    private void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        var fileIgnored = false;
        var file = PathBuilder.From(e.FullPath);
        var buildConfigFile = this.inputPath / "build";
        var buildConfiguration = Serializer.DeserializeExt<BuildConfiguration>(buildConfigFile);
        var relativeToOutput = file.IsRelativeTo(this.outputPath);
        var relativeToInput = file.GetRelativeTo(this.inputPath);
        if (buildConfiguration?.SitePaths != null)
            foreach (var site in buildConfiguration.SitePaths)
            {
                var sitePath = this.inputPath / site.Path;
                if (!file.IsRelativeTo(sitePath)) continue;
                var relativeToSite = file.GetRelativeTo(sitePath);
                var configFile = sitePath / "site";
                var siteConfiguration = Serializer.DeserializeExt<SiteConfiguration>(configFile);
                if (siteConfiguration?.Derived != null) foreach (var ignoredGlob in siteConfiguration.Derived)
                    {
                        if (Glob.IsMatch(relativeToSite, ignoredGlob))
                        {
                            fileIgnored = true;
                        }
                    }
            }

        if (!relativeToOutput && !fileIgnored)
        {
            var logger = this.serviceProvider.GetService<ILogger<VergeExecutor>>();
            logger?.LogInformation("{filename} changed, initiating site rebuild.", file);
            Rebuild();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                exiting = true;
                while (this.rebuildThread?.IsAlive ?? false)
                {
                    Thread.Sleep(100);
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public static class Program
{
    public static async Task Main(string[] args)
    {
        await VergeExecutor.Start(args, null);
    }
}
