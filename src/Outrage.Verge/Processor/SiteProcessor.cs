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
        private readonly PathBuilder outputPath;
        private readonly RenderContext renderContext;

        public SiteProcessor(string rootPath, string outputPath, IServiceProvider serviceProvider)
        {
            this.rootPath = PathBuilder.From(rootPath);
            if (!this.rootPath.IsDirectory)
                throw new ArgumentException($"{rootPath} was not found.");

            this.renderContext = new RenderContext(serviceProvider, rootPath);
            this.outputPath = PathBuilder.From(outputPath);
            this.outputPath.CreateDirectory();

        }

        public void Process()
        {
            HashSet<string> writtenFiles = new();
            ProcessContentFiles(writtenFiles);
            CopyFiles(writtenFiles);
            CleanUpUnwrittenFiles(writtenFiles);

            CleanUpEmptyDirectories();
        }

        private void CopyFiles(HashSet<string> writtenFiles)
        {
            foreach (var copyInstruction in this.renderContext.SiteConfiguration.Copy)
            {
                var toPath = outputPath / copyInstruction.To;
                if (toPath.IsFile)
                {
                    throw new ArgumentException($"{toPath} can not be a file, it must be a folder.");
                }
                var fromPath = this.rootPath / copyInstruction.From;
                if (fromPath.IsFile)
                {
                    var writtenFile = fromPath.CopyToFolder(toPath);
                    writtenFiles.Add(writtenFile);
                }
                if (fromPath.IsDirectory)
                {
                    var copyFiles = Glob.Files(fromPath, copyInstruction.Glob);
                    foreach (var copyFile in copyFiles)
                    {
                        var fromFile = fromPath / copyFile;
                        var writtenFile = fromFile.CopyToFolder(toPath);
                        writtenFiles.Add(writtenFile);
                    }
                }
            }
        }

        private void ProcessContentFiles(HashSet<string> writtenFiles)
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
                        var pageProcessorFactory = this.renderContext.ProcessorFactory.Get(pageFile.Extension);
                        if (pageProcessorFactory != null)
                        {
                            var pageName = pageFile.GetRelativeTo(pagePath);
                            var pageProcessor = pageProcessorFactory.BuildProcessor(pageFile, this.renderContext);
                            var pageWriter = pageProcessorFactory.BuildContentWriter();
                            var (writtenFile, contentStream) = pageWriter.Write(pageName, pageFile, outputPath);
                            using (contentStream) { pageProcessor.RenderToStream(contentStream); }
                            writtenFiles.Add(writtenFile);
                        }
                        else
                        {
                            throw new ArgumentException($"Unsupported file extension {pageFile.Extension}.");
                        }
                    }
                }
            }
        }

        private void CleanUpUnwrittenFiles(HashSet<string> writtenFiles)
        {
            foreach (var file in this.outputPath.ListFiles(options: new EnumerationOptions { RecurseSubdirectories = true }))
            {
                if (!writtenFiles.Contains(file))
                    file.Delete();
            }
        }

        private void CleanUpEmptyDirectories()
        {
            foreach (var directory in this.outputPath.ListDirectories(options: new EnumerationOptions { RecurseSubdirectories = true }))
            {
                if (directory.ListFiles(options: new EnumerationOptions { RecurseSubdirectories = true }).Count() == 0)
                    directory.Delete(true);
            }
        }
    }
}
