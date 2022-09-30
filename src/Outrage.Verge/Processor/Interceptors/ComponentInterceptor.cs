using Microsoft.Extensions.Logging;
using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var innerTokens = renderContext.ContentLibrary.GetHtml(fallbackContentName);

            var childRenderContext = renderContext.CreateChildContext(localAttributes: openTag.Attributes);
            List<IToken> componentTokens = new List<IToken>();
            if (innerTokens.Any(r => (r as OpenTagToken)?.NodeName == "Slot"))
            {
                var singleSlot = innerTokens.Where(r => r is OpenTagToken).Cast<OpenTagToken>().Where(r => r.NodeName == "Slot").Count() == 1;
                var enumerator = innerTokens.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is OpenTagToken && ((OpenTagToken)enumerator.Current).NodeName == "Slot")
                    {
                        var slotTag = (OpenTagToken)enumerator.Current;
                        if (!slotTag.Closed)
                            throw new ArgumentException($"Slot tag must be closed, processing {contentName}");
                        var slotName = slotTag.GetAttributeValue("name");
                        var tokenGroup = GetTokenGroup(tokens, slotName);
                        if (tokenGroup != null)
                            componentTokens.AddRange(tokenGroup);
                        else if (singleSlot && slotName == "Body")
                        {
                            componentTokens.AddRange(tokens);
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

        public IEnumerable<IToken>? GetTokenGroup(IEnumerable<IToken> tokens, string nodeName)
        {
            var result = new Dictionary<string, IEnumerable<IToken>>();
            var enumerable = new TokenEnumerator(tokens);
            while (enumerable.MoveNext())
            {
                if (enumerable.Current is OpenTagToken)
                {
                    var openToken = (OpenTagToken)enumerable.Current;
                    if (openToken.NodeName == nodeName && openToken.Attributes.Count == 0)
                    {
                        if (openToken.Closed)
                        {
                            return Enumerable.Repeat(openToken, 1);
                        }
                        else
                        {
                            return enumerable.TakeUntil<CloseTagToken>((closeToken) => closeToken is CloseTagToken && closeToken.NodeName == nodeName);
                        }
                    } else
                    {
                        enumerable.TakeUntil<CloseTagToken>((closeToken) => closeToken.NodeName == openToken.NodeName);
                    }
                }
            }

            return null;
        }

    }
}
