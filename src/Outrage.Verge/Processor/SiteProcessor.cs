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
        private readonly ContentLibrary contentLibrary;
        private readonly InterceptorFactory interceptorFactory;
        private readonly ThemesFactory themesFactory;
        private readonly PathBuilder outputPath;
        private readonly SiteConfiguration siteConfiguration;
        private readonly HashSet<string> writtenFiles = new();
        private readonly Variables variables;
        static Regex htmlPageNameExpression = new Regex("^(?<name>.*?)[.]html$", RegexOptions.Compiled);

        public SiteProcessor(string rootPath, string outputPath, IServiceProvider? serviceProvider)
        {
            this.rootPath = new PathBuilder(rootPath);
            if (!this.rootPath.IsDirectory)
                throw new ArgumentException($"{rootPath} was not found.");

            this.contentLibrary = new ContentLibrary(rootPath);
            this.siteConfiguration = this.contentLibrary.Deserialize<SiteConfiguration>("site");
            this.interceptorFactory = new InterceptorFactory(this.contentLibrary, serviceProvider);
            this.themesFactory = new ThemesFactory(this.contentLibrary, this.siteConfiguration.ThemesPath);
            this.outputPath = new PathBuilder(outputPath);
            this.outputPath.CreateDirectory();

            this.variables = new Variables(new Dictionary<string, string>
            {
                {"themeTemplate", themesFactory.GetThemeLayout(this.siteConfiguration.Theme) },
                {"themeBase", $"{siteConfiguration.ThemesPath}/{siteConfiguration.Theme}" }
            });
        }

        public void Process()
        {
            var files = this.rootPath.ListContents(options: new EnumerationOptions { RecurseSubdirectories = true });

            foreach (var pagePathString in siteConfiguration.PagePaths)
            {
                var pagePath = this.rootPath / pagePathString;
                foreach (var pageGlob in this.siteConfiguration.PageGlobs)
                {
                    var pageFiles = Glob.Files(pagePath, pageGlob);

                    foreach (var pageFileString in pageFiles)
                    {
                        var pageFile = pagePath / pageFileString;
                        if (pageFile.Extension == ".html")
                        {
                            var pageName = pageFile.GetRelativeTo(pagePath);
                            ProcessHTMLPage(pageName, pageFile);
                        }
                        else
                        {
                            throw new ArgumentException($"Unsupported file extension {pageFile.Extension}.");
                        }
                    }
                }
            }

            CleanUpUnwrittenFiles();

            CleanUpEmptyDirectories();
        }

        private void ProcessHTMLPage(string pageName, PathBuilder pageFile)
        {
            if (pageName.EndsWith(".html") && pageName != "index.html")
            {
                var match = htmlPageNameExpression.Match(pageName);
                if (match.Success)
                {
                    pageName = match.Groups["name"] + "/index.html";
                }
            }
            var pageProcessor = new PageProcessor(pageFile, this.contentLibrary, this.interceptorFactory, this.variables);
            var content = pageProcessor.Render();

            var outputFile = this.outputPath / pageName;
            var outputFolder = outputFile.GetDirectory();
            if (!outputFolder.IsDirectory) outputFolder.CreateDirectory();
            outputFile.Write(content);
            writtenFiles.Add(pageName);
            
        }

        private void CleanUpUnwrittenFiles()
        {
            foreach (var file in this.outputPath.ListFiles(options: new EnumerationOptions { RecurseSubdirectories = true }))
            {
                var relative = file.GetRelativeTo(this.outputPath);
                if (!writtenFiles.Contains(relative))
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
