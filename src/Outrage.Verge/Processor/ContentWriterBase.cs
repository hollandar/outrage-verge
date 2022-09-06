using Compose.Path;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public abstract class ContentWriterBase : IContentWriter
    {
        public virtual string BuildUri(ContentName contentName)
        {
            var uri = contentName.Value;
            if (uri.LastIndexOf(".") > -1)
            {
                uri = uri.Substring(0, uri.LastIndexOf('.'));
            }

            if (uri.EndsWith("index"))
            {
                uri = uri.Substring(0, uri.Length - 5);
            }

            if (uri.EndsWith("/"))
            {
                uri = uri.TrimEnd('/');
            }

            if (!uri.StartsWith('/'))
            {
                uri = "/" + uri;
            }

            return uri;
        }

        public abstract Stream Write(string pageName, PathBuilder pagePath, PathBuilder outputPath);
    }
}
