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
            if (this.buildContext.ExecuteSetup && this.renderContext.SiteConfiguration.Exec != null)
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

        private async Task ExecuteAsync(ContentName folder, ICollection<BuildCommand>? cmds)
        {
            if (cmds != null)
                foreach (var cmd in cmds)
                {
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
            foreach (var copyInstruction in this.renderContext.SiteConfiguration.Copy)
            {
                ArgumentNullException.ThrowIfNull(copyInstruction.From);
                var files = this.renderContext.ContentLibrary.ListContent(copyInstruction.Glob, this.rootPath / copyInstruction.From);
                foreach (var file in files)
                {
                    using var fromStream = this.renderContext.ContentLibrary.OpenStream(this.rootPath / copyInstruction.From / file);
                    using var toStream = this.renderContext.PublishLibrary.OpenStream($"{copyInstruction.To}{file}");

                    await fromStream.CopyToAsync(toStream);
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
