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

        public Task<IEnumerable<IToken>?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            if (openTag.HasAttribute("headline"))
            {
                var headline = openTag.GetAttributeValue("headline");

                IToken[] resultTokens = new IToken[] {
                    new OpenTagToken("h1"),
                    new StringValueToken(headline),
                    new CloseTagToken("h1"),
                };

                return Task.FromResult<IEnumerable<IToken>?>(resultTokens);
            }

            return Task.FromResult<IEnumerable<IToken>?>(null);
        }
    }
}
