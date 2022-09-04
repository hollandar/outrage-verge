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
        public string GetTag()
        {
            return "Include";
        }

        public IEnumerable<IToken>? Render(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var contentName = openTag.GetAttributeValue<string>("name");
            if (!renderContext.ContentLibrary.ContentExists(contentName))
                throw new ArgumentException($"No content with the name {contentName} exists.");

            var pageProcessor = new HtmlProcessor(contentName, renderContext);
            pageProcessor.RenderToStream(writer);

            return Enumerable.Empty<IToken>();
        }
    }
}
