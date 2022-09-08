using Compose.Path;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Html
{
    internal class HtmlWriter : ContentWriterBase
    {
        static readonly Regex htmlPageNameExpression = new Regex("^(?<name>.*?)[.]html$", RegexOptions.Compiled);
        private readonly RenderContext renderContext;

        public HtmlWriter(RenderContext renderContext)
        {
            this.renderContext = renderContext;
        }

        public override async Task<Stream> Write(ContentName pageName, PathBuilder outputPath)
        {
            if (pageName.Value.EndsWith(".html") && pageName.Filename != "index.html")
            {
                var match = htmlPageNameExpression.Match(pageName);
                if (match.Success)
                {
                    pageName = match.Groups["name"] + "/index.html";
                }
            }

            await renderContext.NotifyContentGenerators(renderContext, BuildUri(pageName), pageName);
            var fileStream = this.renderContext.PublishLibrary.OpenStream(pageName);
            return fileStream;
        }

        
    }
}
