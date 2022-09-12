using Microsoft.Extensions.Logging;
using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Interceptors
{
    public class ComponentInterceptor : IInterceptor
    {
        private IDictionary<string, string>? componentStructureMap = null;

        private void GetComponentStructureMap(RenderContext renderContext)
        {
            if (componentStructureMap == null)
            {
                componentStructureMap = renderContext.GetComponentMappings();
            }
        }

        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            GetComponentStructureMap(renderContext);
            return tagName == "Component" || (componentStructureMap?.ContainsKey(tagName) ?? false);
        }

        public Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            string contentName;
            GetComponentStructureMap(renderContext);
            if (componentStructureMap?.ContainsKey(openTag.NodeName) ?? false)
            {
                contentName = componentStructureMap[openTag.NodeName];
            } else
            {
                contentName = openTag.GetAttributeValue<string>("name");
            }

            var fallbackContentName = renderContext.GetFallbackContent(contentName);
            var innerSections = renderContext.GetTokenGroups(tokens);
            var innerTokens = renderContext.ContentLibrary.GetHtml(fallbackContentName);

            List<IToken> componentTokens = new List<IToken>();
            var enumerator = innerTokens.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is OpenTagToken && ((OpenTagToken)enumerator.Current).NodeName == "Slot") {
                    var slotTag = (OpenTagToken)enumerator.Current;
                    if (!slotTag.Closed)
                        throw new ArgumentException($"Slot tag must be closed, processing {contentName}");
                    var slotName = slotTag.GetAttributeValue("name");
                    if (innerSections.ContainsKey(slotName))
                    {
                        componentTokens.AddRange(innerSections[slotName]);
                    }
                }
                else
                {
                    componentTokens.Add(enumerator.Current);
                }
            }

            return Task.FromResult<InterceptorResult?>(new InterceptorResult(componentTokens));
        }
    }
}
