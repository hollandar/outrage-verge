using Microsoft.Extensions.Logging;
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

        public async Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            string contentName;
            GetComponentStructureMap(renderContext);
            if (componentStructureMap?.ContainsKey(openTag.NodeName) ?? false)
            {
                contentName = componentStructureMap[openTag.NodeName];
            }
            else
            {
                contentName = openTag.GetAttributeValue<string>("name");
            }

            var fallbackContentName = renderContext.GetFallbackContent(contentName);
            var innerSections = renderContext.GetTokenGroups(tokens);
            var innerTokens = renderContext.ContentLibrary.GetHtml(fallbackContentName);

            var childRenderContext = renderContext.CreateChildContext();
            var otherAttributes = openTag.Attributes.Where(r => r.AttributeName != "name");
            foreach (var otherAttribute in otherAttributes)
            {
                childRenderContext.Variables.SetValue(otherAttribute.AttributeName, renderContext.Variables.ReplaceVariables(otherAttribute.AttributeValue));
            }

            List<IToken> componentTokens = new List<IToken>();
            if (innerTokens.Any(r => (r as OpenTagToken)?.NodeName == "Slot"))
            {
                var enumerator = innerTokens.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is OpenTagToken && ((OpenTagToken)enumerator.Current).NodeName == "Slot")
                    {
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
            }
            else
                componentTokens.AddRange(innerTokens);

            var htmlProcessor = new HtmlProcessor(componentTokens, childRenderContext);
            await htmlProcessor.RenderToStream(writer);

            return null;
        }
    }
}
