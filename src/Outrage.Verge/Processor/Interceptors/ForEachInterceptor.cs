﻿using Compose.Serialize;
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
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "ForEach";
        }

        public async Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
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

                        var variables = Variables.Empty;
                        variables.SetValue(name, enumerator.Current);
                        var nrc = renderContext.CreateChildContext(openTag.Attributes, variables);
                        var processor = new HtmlProcessor(tokens, nrc);
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
