using Compose.Path;
using Microsoft.VisualBasic;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class RenderContext
    {
        private readonly IDictionary<string, string> fallbackCache = new Dictionary<string, string>();

        public RenderContext(IServiceProvider serviceProvider, PathBuilder rootPath)
        {
            ContentLibrary = new ContentLibrary(rootPath);
            SiteConfiguration = this.ContentLibrary.Deserialize<SiteConfiguration>("site");
            InterceptorFactory = new InterceptorFactory(this.ContentLibrary, serviceProvider);
            ThemesFactory = new ThemesFactory(this.ContentLibrary, this.SiteConfiguration.ThemesPath);
            ProcessorFactory = new ProcessorFactory(serviceProvider);

            var variables = new Dictionary<string, object>
            {
                {"themeTemplate", ThemesFactory.GetThemeLayout(SiteConfiguration.Theme) },
                {"themeBase", $"{SiteConfiguration.ThemesPath}/{SiteConfiguration.Theme}" }
            };

            foreach (var variable in SiteConfiguration.Variables)
            {
                variables[variable.Name] = variable.Value;
            }

            Variables = new Variables(variables);

        }

        private RenderContext(ContentLibrary contentLibrary, InterceptorFactory interceptorFactory, SiteConfiguration siteConfiguration, ThemesFactory themesFactory,
            ProcessorFactory processorFactory, Variables variables)
        {
            ContentLibrary = contentLibrary;
            InterceptorFactory = interceptorFactory;
            SiteConfiguration = siteConfiguration;
            ThemesFactory = themesFactory;
            ProcessorFactory = processorFactory;
            Variables = variables;
        }

        public RenderContext CreateChildContext(Variables variables)
        {
            var renderContext = new RenderContext(ContentLibrary,
                InterceptorFactory,
                SiteConfiguration,
                ThemesFactory,
                ProcessorFactory,
                Variables.Combine(variables)
            );
            return renderContext;
        }

        public SiteConfiguration SiteConfiguration { get; set; }
        public ContentLibrary ContentLibrary { get; set; }
        public InterceptorFactory InterceptorFactory { get; set; }
        public ProcessorFactory ProcessorFactory { get; set; }
        public ThemesFactory ThemesFactory { get; set; }
        public Variables Variables { get; set; }

        public string GetFallbackContent(string contentName)
        {
            var contentTarget = String.Empty;
            if (this.fallbackCache.TryGetValue(contentName, out contentTarget))
            {
                return contentTarget;
            }

            contentTarget = contentName;

            if (!this.ContentLibrary.ContentExists(contentTarget))
            {
                
                foreach (var fallback in this.SiteConfiguration.LocationFallbacks)
                {
                    var fallbackContentName = this.Variables.ReplaceVariables($"{fallback}/{contentName}");

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
    }
}
