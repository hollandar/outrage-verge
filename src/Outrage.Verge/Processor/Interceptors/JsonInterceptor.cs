using Compose.Serialize;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace Outrage.Verge.Processor.Interceptors
{
    public class JsonInterceptor : IInterceptor
    {
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "Json";
        }

        public async Task<InterceptorResult?> RenderAsync(HtmlProcessor parentProcessor, RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var name = openTag.GetAttributeValue<string>("name");
            var from = openTag.GetAttributeValue<string>("from");
            var fromVariable = renderContext.Variables.ReplaceVariables(from);

            var content = renderContext.ContentLibrary.GetContentString(renderContext.GetRelativeContentName(fromVariable));

            var model = JsonConvert.DeserializeObject<JObject>(content);
            if (openTag.Closed)
            {
                renderContext.Variables.SetValue(name, model);
            } else
            {
                var variables = Variables.Empty;
                variables.SetValue(name, model);
                var nrc = renderContext.CreateChildContext(openTag.Attributes, variables);
                var processor = parentProcessor.MakeChild(tokens, nrc);
                await processor.RenderToStream(writer);
            }

            return null;
        }
    }
}
