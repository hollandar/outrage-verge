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

        public async Task<InterceptorResult?> RenderAsync(HtmlProcessor parentProcessor, RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var name = openTag.GetAttributeValue<string>("name");
            var value = openTag.GetAttributeValue<string>("value");
            var ifUndefined = openTag.HasAttribute("if-undefined");
            var defaultTo = openTag.GetAttributeValue<string?>("default");

            var definitionValue = renderContext.Variables.ReplaceVariables(value);
            if (String.IsNullOrWhiteSpace(definitionValue))
                definitionValue = defaultTo ?? "";

            if (openTag.Closed)
            {
                if ((ifUndefined && !renderContext.Variables.HasValue(name)) || !ifUndefined)
                    renderContext.Variables.SetValue(name, definitionValue);
            }
            else
            {
                var variables = Variables.Empty;

                if ((ifUndefined && !renderContext.Variables.HasValue(name)) || !ifUndefined)
                    variables.SetValue(name, definitionValue);

                var nrc = renderContext.CreateChildContext(openTag.Attributes, variables);
                var processor = parentProcessor.MakeChild(tokens, nrc);
                await processor.RenderToStream(writer);
            }

            return null;
        }
    }
}
