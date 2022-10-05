using Compose.Serialize;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Outrage.TokenParser;
using Outrage.Verge.Extensions;
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
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "ForEach";
        }

        public async Task<InterceptorResult?> RenderAsync(HtmlProcessor parentProcessor, RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var name = openTag.GetAttributeValue<string>("name");
            var from = openTag.GetAttributeValue<string>("in");
            var skip = openTag.GetAttributeValue<int?>("skip");
            var take = openTag.GetAttributeValue<int?>("take");

            var headerTokens = tokens.GetInnerTokens("Header");
            var templateTokens = tokens.GetInnerTokens("ItemTemplate");
            var noneTokens = tokens.GetInnerTokens("NotFound");

            if (headerTokens.Count == 0 && templateTokens.Count == 0 && noneTokens.Count == 0)
                templateTokens = tokens.ToList();
            
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
                    var skipIndex = -1;
                    var takenCount = 0;
                    var headerRendered = false;
                    while (enumerator.MoveNext())
                    {

                        skipIndex++;
                        if (skipIndex < (skip ?? 0))
                        {
                            continue;
                        }

                        if (headerTokens.Any() && !headerRendered)
                        {
                            var processor = parentProcessor.MakeChild(headerTokens, renderContext);
                            await processor.RenderToStream(writer);
                            headerRendered = true;
                        }

                        if (takenCount < (take ?? int.MaxValue))
                        {
                            var variables = Variables.Empty;
                            variables.SetValue(name, enumerator.Current);
                            var nrc = renderContext.CreateChildContext(openTag.Attributes, variables);
                            var processor = parentProcessor.MakeChild(templateTokens, nrc);
                            await processor.RenderToStream(writer);
                            takenCount++;
                        }
                    }

                    if (takenCount == 0)
                    {
                        var processor = parentProcessor.MakeChild(noneTokens, renderContext);
                        await processor.RenderToStream(writer);
                    }
                }
            }
            else
            {
                throw new ArgumentException($"No variable with name {from} is defined.");
            }

            return null;
        }
    }
}
