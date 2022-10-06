using Compose.Path;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Outrage.TokenParser;
using Outrage.Verge.Build;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using Outrage.Verge.Parser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class RenderContext : ILogger<RenderContext>
    {

        private readonly ContentName rootPath;
        private readonly BuildContext buildContext;
        private readonly IEnumerable<IContentGenerator>? contentGenerators;
        private readonly ILogger<RenderContext>? logger;

        public RenderContext(BuildContext buildContext, ContentName rootPath, PathBuilder publishPath, IEnumerable<IContentGenerator>? contentGenerators)
        {
            this.rootPath = rootPath;
            this.buildContext = buildContext;
            PublishLibrary = new PublishLibrary(buildContext.ContentLibrary.RootPath / publishPath);
            var siteConfiguration = this.ContentLibrary.Deserialize<SiteConfiguration>(rootPath / "site");
            if (siteConfiguration == null)
            {
                throw new ArgumentException("Could not deserialize site configuration (site.json/site.yaml).");
            }
            SiteConfiguration = siteConfiguration;
            InterceptorFactory = new InterceptorFactory(this.buildContext.ServiceProvider);
            ProcessorFactory = new ProcessorFactory(this.buildContext.ServiceProvider);

            var variables = new Dictionary<string, object?>();
            if (SiteConfiguration.Theme != null)
            {
                var theme = this.buildContext.ThemesFactory.Get(SiteConfiguration.Theme);
                if (theme == null)
                    throw new Exception($"A theme configuration could not be loaded for theme {SiteConfiguration.Theme}");

                variables["themeName"] = SiteConfiguration.Theme;
                variables["themeBase"] = theme.ThemeBase;
            }
            foreach (var variable in SiteConfiguration.Variables)
            {
                if (variable.Name != null)
                    variables[variable.Name] = variable.Value;
            }
            variables["language"] = SiteConfiguration.Language;

            Variables = new Variables(variables);
            this.contentGenerators = contentGenerators;
            this.logger = this.buildContext.ServiceProvider.GetService<ILogger<RenderContext>>();
        }

        private RenderContext(RenderContext renderContext, Variables variables)
        {
            this.rootPath = renderContext.rootPath;
            this.buildContext = renderContext.buildContext;
            PublishLibrary = renderContext.PublishLibrary;
            InterceptorFactory = renderContext.InterceptorFactory;
            SiteConfiguration = renderContext.SiteConfiguration;
            ProcessorFactory = renderContext.ProcessorFactory;
            this.contentGenerators = renderContext.contentGenerators;
            this.logger = renderContext.logger;

            Variables = variables;
        }

        public RenderContext CreateChildContext(List<AttributeToken>? localAttributes = null, Variables? localVariables = null)
        {
            var attributeVariables = Variables.Empty;
            var childVariables = localVariables ?? Variables.Empty;
            if (localAttributes != null) foreach (var token in localAttributes)
            {
                if (!attributeVariables.HasValue(token.AttributeName))
                {
                    attributeVariables.SetValue(token.AttributeName, token.AttributeValue);
                }
            }
            var variables = new Variables(childVariables, this.Variables, attributeVariables);
            var renderContext = new RenderContext(
                this, variables
            );
            return renderContext;
        }

        public BuildConfiguration? BuildConfiguration => buildContext.BuildConfiguration;
        public SiteConfiguration SiteConfiguration { get; set; }
        public PublishLibrary PublishLibrary { get; set; }
        public ContentLibrary ContentLibrary => this.buildContext.ContentLibrary;
        public InterceptorFactory InterceptorFactory { get; set; }
        public ProcessorFactory ProcessorFactory { get; set; }
        public Variables Variables { get; set; }
        public ILogger? Logger { get { return this.logger; } }

        public ContentName GetRelativeContentName(ContentName contentName)
        {
            return this.rootPath / contentName;
        }

        public async Task NotifyContentGenerators(RenderContext renderContext, string contentUri, ContentName contentName)
        {
            if (contentGenerators != null) foreach (var generator in contentGenerators)
                {
                    await generator.ContentUpdated(renderContext, contentUri, contentName);
                }
        }

        public async Task RenderComponent(ContentName componentName, StreamWriter writer)
        {
            var componentContent = ProcessorFactory.Get(componentName.Extension);
            var processor = componentContent.BuildProcessor(componentName, this);
            await processor.RenderToStream(writer);
        }


        public IDictionary<string, string> GetComponentMappings()
        {
            return this.buildContext.GetComponentMappings(this.Variables);
        }

        public ContentName GetFallbackContent(ContentName contentName)
        {
            if (this.ContentLibrary.ContentExists(contentName))
            {
                return contentName;
            }

            var siteContentName = this.rootPath / contentName;
            if (this.ContentLibrary.ContentExists(siteContentName))
            {
                return siteContentName;
            }

            return this.buildContext.GetFallbackContent(contentName, this.Variables);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            this.logger?.Log(logLevel, eventId, state, exception, formatter);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return this.logger?.IsEnabled(logLevel) ?? false;
        }

        class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this.logger?.BeginScope(state) ?? new EmptyDisposable();
        }
    }
}
