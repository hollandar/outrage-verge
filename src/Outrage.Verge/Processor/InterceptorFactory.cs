using Outrage.Verge.Parser.Tokens;
using Outrage.TokenParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Outrage.Verge.Library;
using Microsoft.Extensions.DependencyInjection;

namespace Outrage.Verge.Processor
{
    public interface IInterceptor
    {
        bool CanHandle(RenderContext renderContext, string name);
        Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer);
    }

    public class InterceptorResult
    {
        public InterceptorResult(IEnumerable<IToken> tokens)
        {
            this.Tokens.AddRange(tokens);
        }
        
        public InterceptorResult() { }

        public string Html { get; set; } = String.Empty;
        public List<IToken> Tokens { get; set; } = new();
    }

    public class InterceptorFactory
    {
        private readonly IDictionary<string, IInterceptor> interceptorCache = new Dictionary<string, IInterceptor>();
        private readonly IEnumerable<IInterceptor> interceptors = Enumerable.Empty<IInterceptor>();

        public InterceptorFactory(IServiceProvider? serviceProvider)
        {
            if (serviceProvider != null)
            {
                this.interceptors = serviceProvider.GetService<IEnumerable<IInterceptor>>() ?? Enumerable.Empty<IInterceptor>();
            }
        }

        private IInterceptor? GetInterceptor(RenderContext renderContext, string tagName)
        {
            if (interceptorCache.ContainsKey(tagName))
                return interceptorCache[tagName];

            foreach (var interceptor in interceptors)
            {
                if (interceptor.CanHandle(renderContext, tagName))
                {
                    interceptorCache.Add(tagName, interceptor);
                    return interceptor;
                }
            }

            return null;
        }

        public bool IsDefined(RenderContext renderContext, string tagName)
        {
            return GetInterceptor(renderContext, tagName) != null;
        }

        public async Task<InterceptorResult?> RenderInterceptorAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            if (!IsDefined(renderContext, openTag.NodeName))
                throw new ArgumentException($"No interceptor is defined for {openTag.NodeName}.");

            var interceptor = GetInterceptor(renderContext, openTag.NodeName);
            return await interceptor!.RenderAsync(renderContext, openTag, tokens, writer);
        }
    }
}
