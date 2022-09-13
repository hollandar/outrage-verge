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
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "Define";
        }

        public async Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var name = openTag.GetAttributeValue<string>("name");
            var value = openTag.GetAttributeValue<object>("value");
            var ifUndefined = openTag.HasAttribute("if-undefined");

            if (openTag.Closed)
            {
                renderContext.Variables.SetValue(name, value);
            }
            else
            {
                var variables = new Variables(renderContext.Variables);
                if ((ifUndefined && !variables.HasValue(name)) || !ifUndefined)
                    variables.SetValue(name, value);

                var nrc = renderContext.CreateChildContext(variables);
                var processor = new HtmlProcessor(tokens, nrc);
                await processor.RenderToStream(writer);
            }

            return null;
        }
    }
}
