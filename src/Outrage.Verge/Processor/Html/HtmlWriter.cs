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

        public override Stream Write(string pageName, PathBuilder pagePath, PathBuilder outputPath)
        {
            if (pageName.EndsWith(".html") && pageName != "index.html")
            {
                var match = htmlPageNameExpression.Match(pageName);
                if (match.Success)
                {
                    pageName = match.Groups["name"] + "/index.html";
                }
            }

            var fileStream = this.renderContext.PublishLibrary.OpenStream(pageName);
            return fileStream;
        }

        
    }
}
