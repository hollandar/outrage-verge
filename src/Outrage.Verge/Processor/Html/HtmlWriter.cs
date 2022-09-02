using Compose.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Html
{
    internal class HtmlWriter : IContentWriter
    {
        static readonly Regex htmlPageNameExpression = new Regex("^(?<name>.*?)[.]html$", RegexOptions.Compiled);

        public (string, Stream) Write(string pageName, PathBuilder pagePath, PathBuilder outputPath)
        {
            if (pageName.EndsWith(".html") && pageName != "index.html")
            {
                var match = htmlPageNameExpression.Match(pageName);
                if (match.Success)
                {
                    pageName = match.Groups["name"] + "/index.html";
                }
            }

            var outputFile = outputPath / pageName;
            var outputFolder = outputFile.GetDirectory();
            if (!outputFolder.IsDirectory) outputFolder.CreateDirectory();

            var fileStream = outputFile.OpenFilestream(FileMode.Create);

            return (outputFile, fileStream);
        }
    }
}
