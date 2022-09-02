using Compose.Path;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public interface IProcessorFactory
    {
        string GetExtension();
        IProcessor BuildProcessor(PathBuilder pageFile, ContentLibrary contentLibrary, InterceptorFactory interceptorFactory, Variables variables);
        IContentWriter BuildContentWriter();
    }
}
