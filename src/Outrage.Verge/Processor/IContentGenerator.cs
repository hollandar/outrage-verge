using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public interface IContentGenerator
    {
        void Reset();
        Task ContentUpdated(RenderContext renderContext, string contentUri, ContentName contentName);
        Task Finalize(RenderContext renderContext);
    }
}
