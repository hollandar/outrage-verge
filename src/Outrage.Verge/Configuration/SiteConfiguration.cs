using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    internal class SiteConfiguration
    {
        public string Name { get; set; }
        public string Theme { get; set; }
        public string ThemesPath { get; set; } = "themes";
        public string[] Copy { get; set; }
        public string[] PagePaths { get; set; } = new string[] { "content" };
        public string[] PageGlobs { get; set; } = new string[] { "**/*.html" };
    }
}
