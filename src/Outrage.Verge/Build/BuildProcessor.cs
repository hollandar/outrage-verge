using Compose.Path;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using Outrage.Verge.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Outrage.Verge.Build
{
    public class BuildProcessor
    {
        private readonly PathBuilder rootPath;
        private readonly ContentLibrary contentLibrary;
        private readonly IServiceProvider serviceProvider;
        private readonly bool executeSetup;
        private readonly BuildConfiguration buildConfiguration;
        private readonly ThemesFactory themesFactory;
        private readonly ILogger<BuildProcessor>? logger;

        public BuildProcessor(PathBuilder rootPath, IServiceProvider serviceProvider, bool executeSetup = false)
        {
            this.rootPath = rootPath;
            this.contentLibrary = new ContentLibrary(rootPath);
            this.serviceProvider = serviceProvider;
            this.executeSetup = executeSetup;
            var loadedBuildConfiguration = contentLibrary.Deserialize<BuildConfiguration>("build");
            if (loadedBuildConfiguration == null)
                throw new Exception("Build configuratioon build.json/build.yaml not found in the site folder.");
            else
                this.buildConfiguration = loadedBuildConfiguration;
            this.themesFactory = new ThemesFactory(this.contentLibrary, this.buildConfiguration.ThemesPath);
            this.logger = serviceProvider.GetService<ILogger<BuildProcessor>>();
        }

        public async Task Process()
        {
            var buildContext = new BuildContext(this.buildConfiguration, this.contentLibrary, this.themesFactory, this.rootPath, this.executeSetup, this.serviceProvider);

            if (this.executeSetup && this.buildConfiguration?.Exec != null)
                await ExecuteAsync(this.buildConfiguration.Exec.Install);
            if (this.buildConfiguration?.Exec != null)
                await ExecuteAsync(this.buildConfiguration.Exec.Prebuild);

            foreach (var site in this.buildConfiguration?.SitePaths ?? Enumerable.Empty<Site>())
            {
                if (site.Path == null || site.Publish == null)
                    throw new Exception("Site path or Publish path is empty in build configuration.");
                var siteProcessor = new SiteProcessor(
                    ContentName.From(site.Path),
                    PathBuilder.From(site.Publish).CombineIfRelative(),
                    buildContext
                    );

                await siteProcessor.Process();
            }

            if (this.buildConfiguration?.Exec != null)
                await ExecuteAsync(this.buildConfiguration.Exec.Postbuild);
        }

        public async Task ExecuteAsync(ICollection<BuildCommand>? cmds)
        {
            if (cmds != null)
                foreach (var cmd in cmds)
                {
                    if (String.IsNullOrWhiteSpace(cmd.Cmd))
                        throw new Exception($"Command is empty during build task execution.");
                    var workingDirectory = this.contentLibrary.RootPath / cmd.In;
                    this.logger?.LogInformation("Executing command {cmd}", cmd);
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
    }
}
