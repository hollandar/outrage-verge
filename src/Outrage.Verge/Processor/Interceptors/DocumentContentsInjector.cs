using Outrage.TokenParser;
using Outrage.TokenParser.Tokens;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;
using Outrage.Verge.Processor.Markdown;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Interceptors
{
    public class DocumentContentsInterceptor : IInterceptor
    {
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "DocumentContents";
        }

        public Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            ContentName? pathAttributeValue = null;
            if (openTag.HasAttribute("path"))
            {
                var pathAttribute = openTag.GetAttributeValue("path");
                pathAttributeValue = ContentName.From(pathAttribute);
            }

            List<IToken> outputTokens = new();
            foreach (var pageGlob in renderContext.SiteConfiguration.PageGlobs)
            {
                var pageFiles = renderContext.ContentLibrary.ListContent(pageGlob, pathAttributeValue);

                foreach (var pageFile in pageFiles)
                {
                    var pageName = pathAttributeValue / pageFile;
                    var contentName = pathAttributeValue / pageFile;
                    var pageProcessorFactory = renderContext.ProcessorFactory.Get(contentName.Extension);
                    if (pageProcessorFactory != null)
                    {
                        var frontmatter = renderContext.ContentLibrary.GetFrontmatter<FrontmatterMarkdown>(contentName);
                        var pageRenderContext = renderContext.CreateChildContext();
                        var pageWriter = pageProcessorFactory.BuildContentWriter(pageRenderContext);
                        var contentUri = pageWriter.BuildUri(pageName);

                        var linkTag = new OpenTagToken("a");
                        linkTag.SetAttributeValue("href", contentUri);

                        var contentTag = new StringValueToken(frontmatter?.Title ?? contentUri);
                        var closeTag = new CloseTagToken("a");

                        outputTokens.Add(linkTag);
                        outputTokens.Add(contentTag);
                        outputTokens.Add(closeTag);
                    }
                }
            }

            return Task.FromResult<InterceptorResult?>(new InterceptorResult(outputTokens));
        }
    }
}
