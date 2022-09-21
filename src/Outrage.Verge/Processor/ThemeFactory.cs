using Compose.Path;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class ThemeContext
    {
        public ThemeContext(ContentName themeBase, ThemeConfiguration configuration)
        {
            this.ThemeBase = themeBase;
            this.Configuration = configuration;
        }

        public ContentName ThemeBase { get; set; }
        public ThemeConfiguration Configuration { get; set; }
    }

    public class ThemesFactory
    {
        private readonly ContentLibrary contentLibrary;
        private readonly ContentName themeBase;

        private readonly Dictionary<string, ThemeConfiguration?> loadedThemes = new();

        public ThemesFactory(ContentLibrary contentLibrary, ContentName themeBase)
        {
            this.contentLibrary = contentLibrary;
            this.themeBase = themeBase;
        }

        public ThemeContext? Get(string? themeName)
        {
            if (themeName == null)
            {
                return null;
            }

            var themeConfigurationName = themeBase / themeName / "theme";
            ThemeConfiguration? themeConfiguration;
            if (!loadedThemes.TryGetValue(themeName, out themeConfiguration))
            {
                themeConfiguration = this.contentLibrary.Deserialize<ThemeConfiguration>(themeConfigurationName);
                if (themeConfiguration == null)
                    throw new Exception($"Could not load theme.json/theme.yaml for theme {themeName}");
                loadedThemes[themeName] = themeConfiguration;
            }

            return new ThemeContext(this.themeBase / themeName, themeConfiguration!);
        }
    }
}
