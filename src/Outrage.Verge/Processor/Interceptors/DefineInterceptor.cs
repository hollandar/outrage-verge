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
    public class DefineInterceptor : IInterceptor
    {
        public string GetTag()
        {
            return "Define";
        }

        public IEnumerable<IToken>? Render(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var name = openTag.GetAttributeValue<string>("name");
            var value = openTag.GetAttributeValue<object>("value");

            if (openTag.Closed)
            {
                renderContext.Variables.SetValue(name, value);
            } else
            {
                var variables = new Variables(renderContext.Variables);
                variables.SetValue(name, value);
                var nrc = renderContext.CreateChildContext(variables);
                var processor = new HtmlProcessor(tokens, nrc);
                processor.RenderToStream(writer);
            }

            return Enumerable.Empty<IToken>();
        }
    }
}
