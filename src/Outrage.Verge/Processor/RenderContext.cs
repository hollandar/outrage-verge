using Compose.Path;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class RenderContext
    {
        private readonly IDictionary<ContentName, ContentName> fallbackCache = new Dictionary<ContentName, ContentName>();
        private readonly IEnumerable<IContentGenerator>? contentGenerators;

        public RenderContext(IServiceProvider serviceProvider, PathBuilder rootPath, PathBuilder publishPath, IEnumerable<IContentGenerator>? contentGenerators)
        {
            PublishLibrary = new PublishLibrary(publishPath);
            ContentLibrary = new ContentLibrary(rootPath);
            var siteConfiguration = this.ContentLibrary.Deserialize<SiteConfiguration>("site");
            if (siteConfiguration == null)
            {
                throw new ArgumentException("Could not deserialize site configuration (site.json/site.yaml).");
            }
            SiteConfiguration = siteConfiguration;
            InterceptorFactory = new InterceptorFactory(this.ContentLibrary, serviceProvider);
            ThemesFactory = new ThemesFactory(this.ContentLibrary, this.SiteConfiguration.ThemesPath);
            ProcessorFactory = new ProcessorFactory(serviceProvider);

            var variables = new Dictionary<string, object?>();
            if (SiteConfiguration.Theme != null && SiteConfiguration.ThemesPath != null)
            {
                variables["themeBase"] = $"{SiteConfiguration.ThemesPath}/{SiteConfiguration.Theme}";
            }
            foreach (var variable in SiteConfiguration.Variables)
            {
                if (variable.Name != null)
                    variables[variable.Name] = variable.Value;
            }

            Variables = new Variables(variables);
            this.contentGenerators = contentGenerators;
        }

        private RenderContext(ContentLibrary contentLibrary, PublishLibrary publishLibrary, InterceptorFactory interceptorFactory, SiteConfiguration siteConfiguration, ThemesFactory themesFactory,
            ProcessorFactory processorFactory, Variables variables, IEnumerable<IContentGenerator>? contentGenerators)
        {
            ContentLibrary = contentLibrary;
            PublishLibrary = publishLibrary;
            InterceptorFactory = interceptorFactory;
            SiteConfiguration = siteConfiguration;
            ThemesFactory = themesFactory;
            ProcessorFactory = processorFactory;
            Variables = variables;
            this.contentGenerators = contentGenerators;
        }

        public RenderContext CreateChildContext(Variables? variables = null)
        {
            var renderContext = new RenderContext(ContentLibrary,
                PublishLibrary,
                InterceptorFactory,
                SiteConfiguration,
                ThemesFactory,
                ProcessorFactory,
                Variables.Combine(variables),
                contentGenerators
            );
            return renderContext;
        }

        public SiteConfiguration SiteConfiguration { get; set; }
        public PublishLibrary PublishLibrary { get; set; }
        public ContentLibrary ContentLibrary { get; set; }
        public InterceptorFactory InterceptorFactory { get; set; }
        public ProcessorFactory ProcessorFactory { get; set; }
        public ThemesFactory ThemesFactory { get; set; }
        public Variables Variables { get; set; }

        public async Task NotifyContentGenerators(RenderContext renderContext, string contentUri, ContentName contentName)
        {
            if (contentGenerators != null) foreach (var generator in contentGenerators)
                {
                    await generator.ContentUpdated(renderContext, contentUri, contentName);
                }
        }
        
        public ContentName GetFallbackContent(ContentName contentName)
        {
            var contentTarget = ContentName.Empty;
            if (this.fallbackCache.TryGetValue(contentName, out contentTarget))
            {
                return contentTarget;
            }

            contentTarget = contentName;

            if (!this.ContentLibrary.ContentExists(contentTarget))
            {
                
                foreach (var fallback in this.SiteConfiguration.LocationFallbacks)
                {
                    var fallbackPath = this.Variables.ReplaceVariables(fallback);
                    if (!String.IsNullOrWhiteSpace(fallbackPath)) { 
                        var fallbackContentName = fallbackPath / contentName;

                        if (this.ContentLibrary.ContentExists(fallbackContentName))
                        {
                            contentTarget = fallbackContentName;
                            break;
                        }
                    }
                }
            }

            if (!this.ContentLibrary.ContentExists(contentTarget))
                throw new ArgumentException($"No content with the name {contentName} was found in the site, or in the fallbacks.");

            this.fallbackCache[contentName] = contentTarget;
            return contentTarget;
        }

        public async Task RenderComponent(ContentName componentName, Variables variables, StreamWriter writer)
        {
            var componentContent = ProcessorFactory.Get(componentName.Extension);
            var childRenderContext = CreateChildContext(variables);
            var processor = componentContent.BuildProcessor(componentName, childRenderContext);
            await processor.RenderToStream(writer);
        }
    }
}
