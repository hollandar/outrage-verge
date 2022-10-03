using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Interceptors
{
    public class IncludeInterceptor : IInterceptor
    {
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "Include";
        }

        public async Task<InterceptorResult?> RenderAsync(HtmlProcessor parentProcessor, RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var contentName = renderContext.GetFallbackContent(openTag.GetAttributeValue<string>("name"));
            var pageProcessor = new HtmlProcessor(contentName, renderContext);
            await pageProcessor.RenderToStream(writer);

            return null;
        }
    }
}
