using Outrage.Verge.Parser.Tokens;
using Outrage.TokenParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    internal class HeadlineInterceptor : IInterceptor
    {
        public void Render(OpenTagToken openTag, IEnumerable<IToken> tokens, StringBuilder builder)
        {
            if (openTag.HasAttribute("headline"))
            {
                var headline = openTag.GetAttributeValue("headline");
                builder.Append($"<h1>{headline}</h1>");
            }
        }
    }
}
