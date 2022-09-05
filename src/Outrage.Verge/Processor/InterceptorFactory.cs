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
        string GetTag();
        Task<IEnumerable<IToken>?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer);
    }
    public class InterceptorFactory
    {
        private readonly ContentLibrary contentLibrary;
        public IDictionary<string, IInterceptor> interceptors = new Dictionary<string, IInterceptor>();

        public InterceptorFactory(ContentLibrary contentLibrary, IServiceProvider? serviceProvider)
        {
            this.contentLibrary = contentLibrary;
            if (serviceProvider != null)
            {
                var interceptors = serviceProvider.GetService<IEnumerable<IInterceptor>>();
                foreach (var interceptor in interceptors)
                {
                    if (this.interceptors.ContainsKey(interceptor.GetTag()))
                    {
                        throw new ArgumentException($"An interceptor with the name {interceptor.GetTag()} is already registered.");
                    }
                    this.interceptors[interceptor.GetTag()] = interceptor;
                }
            }
        }

        public bool IsDefined(string tagName)
        {
            return this.interceptors.ContainsKey(tagName);
        }

        public async Task<IEnumerable<IToken>?> RenderInterceptorAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            if (!IsDefined(openTag.NodeName))
                throw new ArgumentException($"No interceptor is defined for {openTag.NodeName}.");

            var interceptor = this.interceptors[openTag.NodeName];
            return await interceptor.RenderAsync(renderContext, openTag, tokens, writer);
        }
    }
}
