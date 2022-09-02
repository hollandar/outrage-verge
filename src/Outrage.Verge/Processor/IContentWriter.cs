using Compose.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public interface IContentWriter
    {
        (string, Stream) Write(string pageName, PathBuilder pagePath, PathBuilder outputPath);
    }
}
