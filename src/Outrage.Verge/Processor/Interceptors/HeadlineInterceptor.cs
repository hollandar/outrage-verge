using Outrage.Verge.Parser.Tokens;
using Outrage.TokenParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Outrage.TokenParser.Tokens;

namespace Outrage.Verge.Processor.Interceptors
{
    public class HeadlineInterceptor : IInterceptor
    {
        public string GetTag() => "Headline";

        public IEnumerable<IToken>? Render(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            if (openTag.HasAttribute("headline"))
            {
                var headline = openTag.GetAttributeValue("headline");

                yield return new OpenTagToken("h1");
                yield return new StringValueToken(headline);
                yield return new CloseTagToken("h1");
            }
        }
    }
}
