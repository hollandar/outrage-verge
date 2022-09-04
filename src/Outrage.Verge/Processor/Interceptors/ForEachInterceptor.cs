using Compose.Serialize;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace Outrage.Verge.Processor.Interceptors
{
    public class ForEachInterceptor : IInterceptor
    {
        public string GetTag()
        {
            return "ForEach";
        }

        public IEnumerable<IToken>? Render(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var name = openTag.GetAttributeValue<string>("name");
            var from = openTag.GetAttributeValue<string>("in");

            if (openTag.Closed)
            {
                throw new ArgumentException("A ForEach tag should not be self closing.");
            }
            else if (renderContext.Variables.HasValue(from))
            {
                var collection = renderContext.Variables.GetValue<dynamic>(from) as IEnumerable;
                if (collection != null)
                {
                    var enumerator = collection.GetEnumerator();
                    while (enumerator.MoveNext())
                    {

                        var variables = new Variables(renderContext.Variables);
                        variables.SetValue(name, enumerator.Current);
                        var nrc = renderContext.CreateChildContext(variables);
                        var processor = new HtmlProcessor(tokens, nrc);
                        processor.RenderToStream(writer);
                    }
                }
            }
            else
            {
                throw new ArgumentException($"No variable with name {from} is defined.");
            }

            return Enumerable.Empty<IToken>();
        }
    }
}
