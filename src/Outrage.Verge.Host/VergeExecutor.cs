
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

    protected VergeExecutor(PathBuilder inputPath, PathBuilder outputPath, IServiceProvider serviceProvider)
    {
        this.inputPath = inputPath;
        this.outputPath = outputPath;
        this.serviceProvider = serviceProvider;

    }

    protected void StopWatching()
    {
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

        if (services == null) services = new ServiceCollection();
        services.AddLogging(options =>
        {
            options.AddConsole();
        });
        services.AddSingleton<IInterceptor, HeadlineInterceptor>();
        services.AddSingleton<IProcessorFactory, HtmlProcessorFactory>();
        services.AddSingleton<IProcessorFactory, MarkdownProcessorFactory>();

        var serviceProvider = services.BuildServiceProvider();
        serveCommand.SetHandler((inputPathValue, outputPathValue) => {
            var inputPath = PathBuilder.From(inputPathValue).CombineIfRelative();
            var outputPath = PathBuilder.From(outputPathValue).CombineIfRelative();

            using (var executor = new VergeExecutor(inputPath, outputPath, serviceProvider))
            {
                executor.RebuildSite();

                if (true)
                {
                    executor.StartWatching();
                    executor.Host(args, services);
                }
            }
        }, inputPathOption, outputPathOption);

        buildCommand.SetHandler((inputPathValue, outputPathValue) =>
        {
            var inputPath = PathBuilder.From(inputPathValue).CombineIfRelative();
            var outputPath = PathBuilder.From(outputPathValue).CombineIfRelative();

            using (var executor = new VergeExecutor(inputPath, outputPath, serviceProvider))
            {
                executor.RebuildSite();
            }
        }, inputPathOption, outputPathOption);

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

                RebuildSite();
            }
            else
            {
                Thread.Sleep(500);
            }
        }
        while (!exiting);
    }

    public void RebuildSite()
    {
        using var scope = this.serviceProvider.CreateScope();

        var siteProcessor = new SiteProcessor(inputPath, outputPath, serviceProvider);
        
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
            siteProcessor.Process();
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
        var file = PathBuilder.From(e.FullPath);
        var relativeToPutput = file.IsRelativeTo(this.outputPath);

        if (!relativeToPutput)
            Rebuild();
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
