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
        private readonly BuildConfiguration? buildConfiguration;
        private readonly ThemesFactory themesFactory;
        private readonly ILogger<BuildProcessor>? logger;

        public BuildProcessor(PathBuilder rootPath, IServiceProvider serviceProvider, bool executeSetup = false)
        {
            this.rootPath = rootPath;
            this.contentLibrary = new ContentLibrary(rootPath);
            this.serviceProvider = serviceProvider;
            this.executeSetup = executeSetup;
            this.buildConfiguration = contentLibrary.Deserialize<BuildConfiguration>("build");
            this.themesFactory = new ThemesFactory(this.contentLibrary, this.buildConfiguration.ThemesPath);
            this.logger = serviceProvider.GetService<ILogger<BuildProcessor>>();
        }

        public async Task Process()
        {
            var buildContext = new BuildContext()
            {
                BuildConfiguration = this.buildConfiguration,
                ContentLibrary = this.contentLibrary,
                ThemesFactory = this.themesFactory,
                RootPath = this.rootPath,
                ExecuteSetup = this.executeSetup,
                ServiceProvider = this.serviceProvider
            };

            if (this.executeSetup && this.buildConfiguration?.Exec != null)
                await ExecuteAsync(this.buildConfiguration.Exec.Install);
            if (this.buildConfiguration?.Exec != null)
                await ExecuteAsync(this.buildConfiguration.Exec.Prebuild);

            foreach (var site in this.buildConfiguration?.SitePaths ?? Enumerable.Empty<Site>())
            {
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
