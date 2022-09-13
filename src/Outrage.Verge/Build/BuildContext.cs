using Compose.Path;
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
    public class BuildContext
    {
        private readonly IDictionary<ContentName, ContentName> fallbackCache = new Dictionary<ContentName, ContentName>();

        public bool ExecuteSetup { get; internal set; }
        public IServiceProvider ServiceProvider { get; internal set; }
        public PathBuilder RootPath { get; internal set; }
        public ContentLibrary ContentLibrary { get; internal set; }
        public ThemesFactory ThemesFactory { get; internal set; }
        public BuildConfiguration? BuildConfiguration { get; internal set; }

        public ContentName GetFallbackContent(ContentName contentName, Variables variables)
        {
            if (this.fallbackCache.TryGetValue(contentName, out var cacheItem))
            {
                return cacheItem;
            }

            var themeVariables = new Variables(variables);
            themeVariables.SetValue("themesPath", this.BuildConfiguration?.ThemesPath);
            var contentTarget = ContentName.Empty;
            foreach (var fallback in this.BuildConfiguration?.FallbackPaths ?? Enumerable.Empty<string>())
            {
                var fallbackPath = themeVariables.ReplaceVariables(fallback);
                if (!String.IsNullOrWhiteSpace(fallbackPath))
                {
                    var fallbackContentName = fallbackPath / contentName;

                    if (this.ContentLibrary.ContentExists(fallbackContentName))
                    {
                        contentTarget = fallbackContentName;
                        break;
                    }
                }
            }

            if (!this.ContentLibrary.ContentExists(contentTarget))
                throw new ArgumentException($"No content with the name {contentName} was found in the site, or in the fallbacks.");

            this.fallbackCache[contentName] = contentTarget;
            return contentTarget;
        }

        public IDictionary<string, string> GetComponentMappings(Variables variables)
        {
            var componentMappings = new Dictionary<string, string>();
            var themeVariables = new Variables(variables);
            themeVariables.SetValue("themesPath", this.BuildConfiguration?.ThemesPath);

            foreach (var fallback in this.BuildConfiguration?.FallbackPaths.Reverse() ?? Enumerable.Empty<String>())
            {
                var fallbackPath = ContentName.From(themeVariables.ReplaceVariables(fallback));
                var fallbackName = fallbackPath / "components";
                try
                {
                    var fallbackComponentMap = this.ContentLibrary.Deserialize<Dictionary<string, string>>(fallbackName);
                    foreach (var map in fallbackComponentMap!) componentMappings[map.Key] = map.Value;
                }
                catch (FileNotFoundException) { }
            }

            try
            {
                var rootComponentMap = this.ContentLibrary.Deserialize<Dictionary<string, string>>("components");
                foreach (var map in rootComponentMap!) componentMappings[map.Key] = map.Value;
            }
            catch (FileNotFoundException) { }

            return componentMappings;
        }


    }
}
