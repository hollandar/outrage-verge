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

namespace Outrage.Verge.Processor.Markdown
{
    public class MarkdownProcessor : ProcessorBase, IProcessor
    {
        public IDictionary<string, string> sectionContent = new Dictionary<string, string>();
        static readonly Regex markdownFrontmatterRegex = new Regex("^(?:(?<frontmatter>.*?)--){0,1}(?<markdown>.*)$", RegexOptions.Singleline | RegexOptions.Compiled);
        const string defaultHeadName = "head";
        const string defaultBodyName = "body";
        const string defaultTemplateName = "markdown.t.html";
        const string defaultTitle = "Document";
        private string content;
        private FrontmatterConfig frontmatter;

        public MarkdownProcessor(string contentName, RenderContext renderContext):base(renderContext)
        {
            Process(contentName);
        }

        protected void Process(string contentName)
        {
            if (!renderContext.ContentLibrary.ContentExists(contentName))
                throw new ArgumentException($"{contentName} is unknown.");

            var markdownFullString = renderContext.ContentLibrary.GetString(contentName);

            var match = markdownFrontmatterRegex.Match(markdownFullString);
            if (match.Success)
            {
                var frontmatterString = match.Groups["frontmatter"].Value ?? String.Empty;
                var markdownString = match.Groups["markdown"].Value ?? String.Empty;

                var yamlDeserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                frontmatter = yamlDeserializer.Deserialize<FrontmatterConfig>(frontmatterString) ?? new();

                content = Markdig.Markdown.ToHtml(markdownString);
                this.sectionContent[frontmatter.Section ?? defaultBodyName] = content;
                this.sectionContent[frontmatter.HeadSection ?? defaultHeadName] = $"<title>{frontmatter.Title ?? defaultTitle}</title>";


                SetTemplate(new OpenTagToken("Template", new AttributeToken("layout", HandleVariables(frontmatter.Template ?? defaultTemplateName))));
            }
        }

        public override void RenderToStream(Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true);
            RenderToStream(writer);
        }

        public override void RenderToStream(StreamWriter stream)
        {
            if (layoutPage != null)
                layoutPage.RenderToStream(stream);
            else
                RenderContent(content, stream);

        }

        protected void RenderContent(string content, StreamWriter writer)
        {
            writer.Write(content);
        }

        public override void RenderSection(OpenTagToken openTag, StreamWriter writer)
        {
            var sectionName = openTag.GetAttributeValue(Constants.SectionNameAtt);
            var sectionExists = this.sectionContent.ContainsKey(sectionName);

            var sectionExpected = false;
            if (openTag.HasAttribute(Constants.SectionRequiredAtt))
                sectionExpected = openTag.GetAttributeValue<bool?>(Constants.SectionRequiredAtt) ?? false;

            if (!sectionExists && sectionExpected)
                throw new ArgumentException($"A section with name {sectionName} has no content, but it was expected.");

            if (sectionExists)
                RenderContent(sectionContent[sectionName], writer);
        }
        
    }

}
