using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;

namespace Outrage.Verge.Processor.Interceptors
{
    public class OptionalInterceptor : IInterceptor
    {
        public bool CanHandle(RenderContext renderContext, string name)
        {
            return name == "Optional";
        }

        public Task<InterceptorResult?> RenderAsync(HtmlProcessor parentProcessor, RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var condition = openTag.GetAttributeValue<string>("if");
            var conditionValue = renderContext.Variables.ReplaceVariables(condition);
            if (!String.IsNullOrWhiteSpace(conditionValue))
            {
                return Task.FromResult<InterceptorResult?>(new InterceptorResult(tokens));
            }

            return Task.FromResult<InterceptorResult?>(null);
        }
    }
}
