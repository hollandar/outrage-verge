using Outrage.TokenParser.Tokens;
using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Markdig.Syntax;
using Compose.Serialize;
using System.IO;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Outrage.Verge.Library;
using Outrage.Verge.Configuration;

namespace Outrage.Verge.Processor.Markdown
{
    public class MarkdownProcessor : ProcessorBase, IProcessor
    {
        public IDictionary<string, string> sectionContent = new Dictionary<string, string>();
        static readonly Regex markdownFrontmatterRegex = new Regex("^(?:(?<frontmatter>.*?)--){0,1}(?<markdown>.*)$", RegexOptions.Singleline | RegexOptions.Compiled);
        private string content = String.Empty;

        public MarkdownProcessor(ContentName contentName, RenderContext renderContext) : base(renderContext)
        {
            Process(contentName);
        }

        protected void Process(ContentName contentName)
        {
            var fallbackContentName = this.renderContext.GetFallbackContent(contentName);
            if (!renderContext.ContentLibrary.ContentExists(fallbackContentName))
                throw new ArgumentException($"{fallbackContentName} is unknown.");

            var contentItem = renderContext.ContentLibrary.GetFrontmatterAndContent<FrontmatterMarkdown>(fallbackContentName);
            content = Markdig.Markdown.ToHtml(contentItem.content);
            this.sectionContent[contentItem.frontmatter.Section] = content;
            contentItem.frontmatter.Apply(this.renderContext.Variables);


            SetDocument(contentItem.frontmatter?.Template, contentItem.frontmatter?.Title);
        }

        public override async Task RenderToStream(Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true);
            await RenderToStream(writer);
        }

        public override async Task RenderToStream(StreamWriter stream)
        {
            if (layoutPage != null)
                await layoutPage.RenderToStream(stream);
            else
                await RenderContent(content, stream);

        }

        protected Task RenderContent(string content, StreamWriter writer)
        {
            writer.Write(content);
            return Task.CompletedTask;
        }

        public override async Task RenderSection(OpenTagToken openTag, StreamWriter writer)
        {
            var sectionName = openTag.GetAttributeValue(Constants.SectionNameAtt);
            if (String.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException($"Section must define a {Constants.SectionNameAtt} attribute, which identifies which section to render in this location.");
            var sectionExists = this.sectionContent.ContainsKey(sectionName);

            var sectionExpected = false;
            if (openTag.HasAttribute(Constants.SectionRequiredAtt))
                sectionExpected = openTag.GetAttributeValue<bool?>(Constants.SectionRequiredAtt) ?? false;

            if (!sectionExists && sectionExpected)
                throw new ArgumentException($"A section with name {sectionName} has no content, but it was expected.");

            if (sectionExists)
                await RenderContent(sectionContent[sectionName], writer);
        }

    }

}
