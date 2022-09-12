using Compose.Path;
using GlobExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly PathBuilder rootPath;
        private readonly PathBuilder publishPath;
        private readonly RenderContext renderContext;
        private readonly IEnumerable<IContentGenerator>? contentGenerators;
        private readonly bool initial;

        public SiteProcessor(string rootPath, string outputPath, IServiceProvider serviceProvider, bool initial = false)
        {
            this.rootPath = PathBuilder.From(rootPath);
            if (!this.rootPath.IsDirectory)
                throw new ArgumentException($"{rootPath} was not found.");

            this.publishPath = PathBuilder.From(outputPath);
            this.contentGenerators = serviceProvider.GetService<IEnumerable<IContentGenerator>>();
            this.renderContext = new RenderContext(serviceProvider, this.rootPath, publishPath, contentGenerators);
            this.initial = initial;
        }

        public async Task Process()
        {
            if (initial && this.renderContext.SiteConfiguration.Exec != null)
                await ExecuteAsync(this.rootPath, this.renderContext.SiteConfiguration.Exec.Install);
            if (this.renderContext.SiteConfiguration.Exec != null)
                await ExecuteAsync(this.rootPath, this.renderContext.SiteConfiguration.Exec.Prebuild);

            if (contentGenerators != null) foreach (var generator in contentGenerators)
                {
                    generator.Reset();
                }

            await CopyContentFiles();
            await BuildContent();
            if (contentGenerators != null) foreach (var generator in contentGenerators)
                {
                    await generator.Finalize(renderContext);
                }

            this.renderContext.PublishLibrary.CleanUp();

            if (this.renderContext.SiteConfiguration.Exec != null)
                await ExecuteAsync(this.rootPath, this.renderContext.SiteConfiguration.Exec.Postbuild);
        }

        private async Task ExecuteAsync(PathBuilder folder, ICollection<string>? cmds)
        {
            if (cmds != null)
                foreach (var cmd in cmds)
                {
                    this.renderContext.LogInformation("Executing command {cmd}", cmd);
                    var argRegex = new Regex("^(?<cmd>.*?)(?:\\s(?<args>.*)$|$)");
                    var match = argRegex.Match(cmd);
                    if (match.Success && match.Groups["cmd"].Success)
                    {
                        var executable = new Executable(match.Groups["cmd"].Value!);
                        if (executable.Exists)
                        {
                            var arguments = match.Groups["args"].Success ? match.Groups["args"].Value : String.Empty;
                            await executable.ExecuteAsync(arguments, folder);
                        }
                    }
                }
        }

        private async Task CopyContentFiles()
        {
            foreach (var copyInstruction in this.renderContext.SiteConfiguration.Copy)
            {
                ArgumentNullException.ThrowIfNull(copyInstruction.From);
                var files = this.renderContext.ContentLibrary.ListContent(copyInstruction.Glob, copyInstruction.From);
                foreach (var file in files)
                {
                    using var fromStream = this.renderContext.ContentLibrary.OpenStream($"{copyInstruction.From}{file}");
                    using var toStream = this.renderContext.PublishLibrary.OpenStream($"{copyInstruction.To}{file}");

                    await fromStream.CopyToAsync(toStream);
                }
            }
        }

        private async Task BuildContent()
        {
            foreach (var PagePath in this.renderContext.SiteConfiguration.PagePaths)
            {
                foreach (var pageGlob in this.renderContext.SiteConfiguration.PageGlobs)
                {
                    var pageFiles = this.renderContext.ContentLibrary.ListContent(pageGlob, PagePath);

                    foreach (var pageFile in pageFiles)
                    {
                        var contentName = PagePath / pageFile;
                        var pageProcessorFactory = this.renderContext.ProcessorFactory.Get(contentName.Extension);
                        if (pageProcessorFactory != null)
                        {
                            this.renderContext.LogInformation("Building {contentName}.", contentName);
                            var pageRenderContext = this.renderContext.CreateChildContext();
                            var pageWriter = pageProcessorFactory.BuildContentWriter(pageRenderContext);
                            var contentUri = pageWriter.BuildUri(pageFile);
                            pageRenderContext.Variables.SetValue("uri", contentUri);
                            var pageProcessor = pageProcessorFactory.BuildProcessor(contentName, pageRenderContext);
                            var contentStream = await pageWriter.Write(pageFile, publishPath);
                            using (contentStream) { await pageProcessor.RenderToStream(contentStream); }
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
