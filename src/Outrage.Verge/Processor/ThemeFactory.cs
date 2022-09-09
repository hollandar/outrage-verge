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
    public class ThemesFactory
    {
        private readonly ContentLibrary contentLibrary;
        private readonly string themeBase;

        private readonly Dictionary<string, ThemeConfiguration> loadedThemes = new();

        public ThemesFactory(ContentLibrary contentLibrary, string themeBase)
        {
            this.contentLibrary = contentLibrary;
            this.themeBase = themeBase;
        }
    }
}
