using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;
using Outrage.Verge.Processor.Markdown;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public abstract class ProcessorBase: IProcessor
    {
        protected readonly RenderContext renderContext;
        
        protected IProcessor? layoutPage;
        protected readonly IProcessor? childPage;

        public ProcessorBase(RenderContext renderContext)
        {
            this.renderContext = renderContext;
        }

        public ProcessorBase(RenderContext renderContext, IProcessor childPage)
        {
            this.renderContext = renderContext;
            this.childPage = childPage;
        }

        protected void SetTemplate(OpenTagToken openToken)
        {
            var templateName = openToken.GetAttributeValue(Constants.TemplateLayoutAtt);
            var templateVariable = HandleVariables(templateName);
            
            if (layoutPage != null)
                throw new ArgumentException("Template page can not be set twice, remove the second template tag.");

            layoutPage = new HtmlProcessor(templateVariable, this, this.renderContext);
        }

        protected string HandleVariables(string input)
        {
            return renderContext.Variables.ReplaceVariables(input);
        }

        public abstract Task RenderToStream(Stream stream);

        public abstract Task RenderToStream(StreamWriter stream);

        public abstract Task RenderSection(OpenTagToken openTag, StreamWriter writer);
    }
}
