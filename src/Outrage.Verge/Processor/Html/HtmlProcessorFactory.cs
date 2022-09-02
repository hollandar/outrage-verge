using Compose.Path;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Html
{
    public class HtmlProcessorFactory : IProcessorFactory
    {
        public string GetExtension() => ".html";

        public IProcessor BuildProcessor(PathBuilder pageFile, ContentLibrary contentLibrary, InterceptorFactory interceptorFactory, Variables variables)
        {
            return new HtmlProcessor(pageFile, contentLibrary, interceptorFactory, variables);
        }

        public IContentWriter BuildContentWriter()
        {
            return new HtmlWriter();
        }
    }
}
