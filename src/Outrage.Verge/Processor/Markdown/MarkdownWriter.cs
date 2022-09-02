using Compose.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Markdown
{
    internal class MarkdownWriter : IContentWriter
    {
        static readonly Regex htmlPageNameExpression = new Regex("^(?<name>.*?)[.]md$", RegexOptions.Compiled);

        public (string, Stream) Write(string pageName, PathBuilder pagePath, PathBuilder outputPath)
        {
            var match = htmlPageNameExpression.Match(pageName);
            if (match.Success)
            {
                if (pageName == "index")
                    pageName = "index.html";
                else
                    pageName = match.Groups["name"] + "/index.html";


                var outputFile = outputPath / pageName;
                var outputFolder = outputFile.GetDirectory();
                if (!outputFolder.IsDirectory) outputFolder.CreateDirectory();

                var fileStream = outputFile.OpenFilestream(FileMode.Create);

                return (outputFile, fileStream);
            }
            else
                throw new ArgumentException($"{pageName} was not a markdown file.");
        }
    }
}
