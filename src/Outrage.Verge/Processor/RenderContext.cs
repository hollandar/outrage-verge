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
        public RenderContext(IServiceProvider serviceProvider, PathBuilder rootPath)
        {
            ContentLibrary = new ContentLibrary(rootPath);
            InterceptorFactory = new InterceptorFactory(this.ContentLibrary, serviceProvider);
            SiteConfiguration = this.ContentLibrary.Deserialize<SiteConfiguration>("site");
            ThemesFactory = new ThemesFactory(this.ContentLibrary, this.SiteConfiguration.ThemesPath);
            ProcessorFactory = new ProcessorFactory(serviceProvider);

            var variables = new Dictionary<string, string>
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
    }
}
