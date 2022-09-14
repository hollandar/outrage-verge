using Outrage.TokenParser;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using Outrage.Verge.Parser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Interceptors
{
    public class MarkdownInterceptor : IInterceptor
    {
        public bool CanHandle(RenderContext renderContext, string name)
        {
            return name == "Markdown";
        }

        public Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var name = openTag.GetAttributeValue<string>("name");
            var fallbackContentName = renderContext.GetFallbackContent(name);
            var markdownFullString = renderContext.ContentLibrary.GetContentString(fallbackContentName);

            var content = Markdig.Markdown.ToHtml(markdownFullString);

            writer.Write(content);

            return Task.FromResult<InterceptorResult?>(null);
        }
    }
}
