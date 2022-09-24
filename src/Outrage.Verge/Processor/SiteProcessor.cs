using Compose.Path;
using GlobExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outrage.Verge.Build;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class SiteProcessor
    {
        private readonly ContentName rootPath;
        private readonly PathBuilder publishPath;
        private readonly BuildContext buildContext;
        private readonly RenderContext renderContext;
        private readonly IEnumerable<IContentGenerator>? contentGenerators;

        public SiteProcessor(ContentName rootPath, PathBuilder outputPath, BuildContext buildContext)
        {
            this.rootPath = rootPath;
            this.publishPath = outputPath;
            this.buildContext = buildContext;
            this.contentGenerators = buildContext.ServiceProvider.GetService<IEnumerable<IContentGenerator>>();
            this.renderContext = new RenderContext(buildContext, this.rootPath, this.publishPath, contentGenerators);
        }

        public async Task Process()
        {

            if (contentGenerators != null) foreach (var generator in contentGenerators)
                {
                    generator.Reset();
                }
            await ExecutePreBuildCommands();
            await CopyContentFiles();
            await BuildContent();
            if (contentGenerators != null) foreach (var generator in contentGenerators)
                {
                    await generator.Finalize(renderContext);
                }

            this.renderContext.PublishLibrary.CleanUp();

            await ExecPostBuildCommands();
        }

        private async Task ExecutePreBuildCommands()
        {
            if (this.buildContext.ExecuteSetup && this.renderContext.SiteConfiguration.Exec != null)
                await ExecuteAsync(this.rootPath, this.renderContext.SiteConfiguration.Exec.Install);
            if (this.renderContext.SiteConfiguration.Exec != null)
                await ExecuteAsync(this.rootPath, this.renderContext.SiteConfiguration.Exec.Prebuild);

            var themeContext = this.buildContext.ThemesFactory.Get(this.renderContext.SiteConfiguration.Theme);
            if (themeContext != null)
            {
                if (this.buildContext.ExecuteSetup && themeContext.Configuration?.Exec != null)
                    await ExecuteAsync(themeContext.ThemeBase, themeContext.Configuration.Exec.Install);
                if (themeContext.Configuration?.Exec != null)
                    await ExecuteAsync(themeContext.ThemeBase, themeContext.Configuration.Exec.Prebuild);
            }
        }

        private async Task ExecPostBuildCommands()
        {
            if (this.renderContext.SiteConfiguration.Exec != null)
                await ExecuteAsync(this.rootPath, this.renderContext.SiteConfiguration.Exec.Postbuild);
            var themeContext = this.buildContext.ThemesFactory.Get(this.renderContext.SiteConfiguration.Theme);
            if (themeContext != null)
            {
                if (themeContext.Configuration?.Exec != null)
                    await ExecuteAsync(themeContext.ThemeBase, themeContext.Configuration.Exec.Postbuild);
            }
        }

            private async Task ExecuteAsync(ContentName folder, ICollection<BuildCommand>? cmds)
        {
            if (cmds != null)
                foreach (var cmd in cmds)
                {
                    if (String.IsNullOrWhiteSpace(cmd.Cmd))
                        throw new Exception($"Command is empty during site task execution {folder}.");

                    var workingDirectory = this.buildContext.ContentLibrary.RootPath / folder / cmd.In;
                    this.renderContext.LogInformation("Executing command {cmd}", cmd);
                    var argRegex = new Regex("^(?<cmd>.*?)(?:\\s(?<args>.*)$|$)");
                    var match = argRegex.Match(cmd.Cmd);
                    if (match.Success && match.Groups["cmd"].Success)
                    {
                        var executable = new Executable(match.Groups["cmd"].Value!);
                        if (executable.Exists)
                        {
                            var arguments = match.Groups["args"].Success ? match.Groups["args"].Value : String.Empty;
                            await executable.ExecuteAsync(arguments, workingDirectory);
                        }
                    }
                }
        }

        
        private async Task CopyContentFiles()
        {
            HashSet<ContentName> copied = new();
            List<CopyItem> copyInstructions = new();
            copyInstructions.AddRange(this.renderContext.SiteConfiguration.Copy);
            var copyFromPath = this.rootPath;
            await CopyWithFallback(copied, copyInstructions, copyFromPath);

            var themeContext = this.buildContext.ThemesFactory.Get(this.renderContext.SiteConfiguration.Theme);
            if (themeContext != null)
            {
                copyFromPath = themeContext.ThemeBase;
                copyInstructions.AddRange(themeContext.Configuration.Copy);
                await CopyWithFallback(copied, copyInstructions, copyFromPath);
            }

            foreach (var library in this.buildContext.LibraryFactories)
            {
                var libraryContext = library.Get();
                if (libraryContext != null)
                {
                    copyFromPath = libraryContext.LibraryBase;
                    copyInstructions.AddRange(libraryContext.LibConfiguration.Copy);
                    await CopyWithFallback(copied, copyInstructions, copyFromPath);
                }
            }
        }

        private async Task CopyWithFallback(HashSet<ContentName> copied, List<CopyItem> copyInstructions, ContentName copyFromPath)
        {
            foreach (var copyInstruction in copyInstructions)
            {
                ArgumentNullException.ThrowIfNull(copyInstruction.From);
                var files = this.renderContext.ContentLibrary.ListContent(copyInstruction.Glob, copyFromPath / copyInstruction.From);
                foreach (var file in files)
                {
                    var componentPath = copyInstruction.From / file;
                    if (!copied.Contains(componentPath))
                    {
                        using var fromStream = this.renderContext.ContentLibrary.OpenStream(copyFromPath / copyInstruction.From / file);
                        using var toStream = this.renderContext.PublishLibrary.OpenPublishStream($"{copyInstruction.To}{file}");

                        await fromStream.CopyToAsync(toStream);
                        copied.Add(componentPath);
                    }
                }
            }
        }

        private async Task BuildContent()
        {
            {

                foreach (var pageGlob in this.renderContext.SiteConfiguration.PageGlobs)
                {
                    var pageFiles = this.renderContext.ContentLibrary.ListContent(pageGlob, rootPath, renderContext.SiteConfiguration.ExcludeGlobs.ToArray());

                    foreach (var pageFile in pageFiles)
                    {
                        var contentName = pageFile;
                        var pageProcessorFactory = this.renderContext.ProcessorFactory.Get(contentName.Extension);
                        if (pageProcessorFactory != null)
                        {
                            var stopwatch = new Stopwatch();
                            stopwatch.Start();
                            var pageRenderContext = this.renderContext.CreateChildContext();
                            var pageWriter = pageProcessorFactory.BuildContentWriter(pageRenderContext);
                            var contentUri = pageWriter.BuildUri(pageFile);
                            pageRenderContext.Variables.SetValue("uri", contentUri);
                            var pageProcessor = pageProcessorFactory.BuildProcessor(contentName, pageRenderContext);
                            var contentStream = await pageWriter.Write(pageFile, publishPath);
                            using (contentStream) { await pageProcessor.RenderToStream(contentStream); }
                            stopwatch.Stop();
                            this.renderContext.LogInformation("Rendered {contentName} in {timeInMilliseconds}.", contentName, stopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            throw new ArgumentException($"Unsupported file extension {contentName.Extension}.");
                        }
                    }
                }
            }
        }
    }
}
