using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    public class BuildConfiguration
    {
        public string ThemesPath { get; set; } = "themes";
        public ICollection<Site> SitePaths { get; set; } = new Site[] { new Site() { Path = "site", Publish = "publish" } };
        public ICollection<string> FallbackPaths { get; set; } = new string[] { "$(themesPath)/$(themeName)", "base" };

        public Exec? Exec { get; set; }
    }
    public class Exec
    {
        public ICollection<BuildCommand>? Install { get; set; } = null;
        public ICollection<BuildCommand>? Prebuild { get; set; } = null;
        public ICollection<BuildCommand>? Postbuild { get; set; } = null;
    }

    public class Site
    {
        public string Path { get; set; }
        public string Publish { get; set; }
    }

    public class BuildCommand
    {
        public string In { get; set; } = "./";
        public string? Cmd { get; set; }
    }


}
