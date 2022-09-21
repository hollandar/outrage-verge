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
    public class RequireInterceptor : IInterceptor
    {
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "Require";
        }

        public Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var name = openTag.GetAttributeValue<string>("name");
            if (!renderContext.Variables.HasValue(name))
                throw new VariableRequiredButNotDefinedException($"The variable {name} is required but is not defined.");
            return Task.FromResult<InterceptorResult?>(null);
        }
    }
}
