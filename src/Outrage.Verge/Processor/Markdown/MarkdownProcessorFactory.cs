using Compose.Path;
using Outrage.Verge.Processor.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Outrage.Verge.Library;

namespace Outrage.Verge.Processor.Markdown
{
    public class MarkdownProcessorFactory : IProcessorFactory
    {
        public string GetExtension() => ".md";

        public IProcessor BuildProcessor(ContentName pageFile, RenderContext renderContext)
        {
            return new MarkdownProcessor(pageFile, renderContext);
        }

        public IContentWriter BuildContentWriter(RenderContext renderContext)
        {
            return new MarkdownWriter(renderContext);
        }
    }
}
