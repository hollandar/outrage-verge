using Compose.Path;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Markdown
{
    internal class MarkdownWriter : ContentWriterBase
    {
        static readonly Regex htmlPageNameExpression = new Regex("^(?<name>.*?)[.]md$", RegexOptions.Compiled);
        private readonly RenderContext renderContext;

        public MarkdownWriter(RenderContext renderContext) {
            this.renderContext = renderContext;
        }

        public override Stream Write(string pageName, PathBuilder pagePath, PathBuilder outputPath)
        {
            var match = htmlPageNameExpression.Match(pageName);
            if (match.Success)
            {
                if (pageName == "index")
                    pageName = "index.html";
                else
                    pageName = match.Groups["name"] + "/index.html";


                var fileStream = this.renderContext.PublishLibrary.OpenStream(pageName);

                return fileStream;
            }
            else
                throw new ArgumentException($"{pageName} was not a markdown file.");
        }
    }
}
