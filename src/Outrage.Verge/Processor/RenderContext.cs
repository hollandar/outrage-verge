﻿using Compose.Path;
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
