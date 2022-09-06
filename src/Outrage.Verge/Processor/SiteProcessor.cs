using Compose.Path;
using GlobExpressions;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
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

        public SiteProcessor(string rootPath, string outputPath, IServiceProvider serviceProvider)
        {
            this.rootPath = PathBuilder.From(rootPath);
            if (!this.rootPath.IsDirectory)
                throw new ArgumentException($"{rootPath} was not found.");

            this.publishPath = PathBuilder.From(outputPath);
            this.renderContext = new RenderContext(serviceProvider, rootPath, publishPath);

        }

        public async Task Process()
        {
            await CopyContentFiles();
            await BuildContent();
            this.renderContext.PublishLibrary.CleanUp();
        }

        private async Task CopyContentFiles()
        {
            foreach (var copyInstruction in this.renderContext.SiteConfiguration.Copy)
            {
                var files = this.renderContext.ContentLibrary.GetContent(copyInstruction.Glob, copyInstruction.From);
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
            foreach (var pagePathString in this.renderContext.SiteConfiguration.PagePaths)
            {
                var pagePath = this.rootPath / pagePathString;
                foreach (var pageGlob in this.renderContext.SiteConfiguration.PageGlobs)
                {
                    var pageFiles = Glob.Files(pagePath, pageGlob);

                    foreach (var pageFileString in pageFiles)
                    {
                        var pageFile = pagePath / pageFileString;
                        var contentName = ContentName.GetContentNameFromRelativePaths(pageFile, rootPath);
                        var pageProcessorFactory = this.renderContext.ProcessorFactory.Get(pageFile.Extension);
                        if (pageProcessorFactory != null)
                        {
                            var pageName = pageFile.GetRelativeTo(pagePath);
                            var pageRenderContext = this.renderContext.CreateChildContext();
                            var pageWriter = pageProcessorFactory.BuildContentWriter(pageRenderContext);
                            var contentUri = pageWriter.BuildUri(contentName);
                            pageRenderContext.Variables.SetValue("uri", contentUri);
                            var pageProcessor = pageProcessorFactory.BuildProcessor(contentName, pageRenderContext);
                            var contentStream = pageWriter.Write(pageName, pageFile, publishPath);
                            using (contentStream) { await pageProcessor.RenderToStream(contentStream); }
                        }
                        else
                        {
                            throw new ArgumentException($"Unsupported file extension {pageFile.Extension}.");
                        }
                    }
                }
            }
        }
    }
}
