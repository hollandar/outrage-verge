using Compose.Path;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public interface IContentWriter
    {
        Task<Stream> Write(ContentName pageName, PathBuilder outputPath);
        string BuildUri(ContentName pageName);
    }
}
