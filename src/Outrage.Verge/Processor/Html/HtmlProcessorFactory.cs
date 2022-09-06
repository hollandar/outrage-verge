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

        public IProcessor BuildProcessor(ContentName pageFile, RenderContext renderContext)
        {
            return new HtmlProcessor(pageFile, renderContext);
        }

        public IContentWriter BuildContentWriter(RenderContext renderContext)
        {
            return new HtmlWriter(renderContext);
        }
    }
}
