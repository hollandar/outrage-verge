using Outrage.TokenParser;
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

        protected void SetDocument(OpenTagToken openTag)
        {
            string? documentTitleString = null;
            if (openTag.HasAttribute(Constants.DocumentTitleAtt)) 
                documentTitleString = openTag.GetAttributeValue<string?>(Constants.DocumentTitleAtt) ?? Constants.DocumentTitleAttDefault;

            string? templateName = null;
            if (openTag.HasAttribute(Constants.DocumentLayoutAtt))
            {
                templateName = openTag.GetAttributeValue<string?>(Constants.DocumentLayoutAtt) ?? Constants.DocumentLayoutAttDefault; 
            }


            SetDocument(templateName, documentTitleString);

        }

        public void SetDocument(string? templateVariable, string? documentTitle)
        {
            if (documentTitle != null || !this.renderContext.Variables.HasValue("title"))
                this.renderContext.Variables.SetValue("title", HandleVariables(documentTitle ?? Constants.DocumentTitleAttDefault));
            
            var template = this.renderContext.GetFallbackContent(HandleVariables(templateVariable ?? Constants.DocumentLayoutAttDefault));

            if (layoutPage != null)
                throw new ArgumentException("Template page can not be set twice, remove the second template tag.");

            layoutPage = new HtmlProcessor(template, this, this.renderContext);
        }

        protected string HandleVariables(string input)
        {
            return renderContext.Variables.ReplaceVariables(input);
        }

        public abstract Task RenderToStream(Stream stream);

        public abstract Task RenderToStream(StreamWriter stream);

        public abstract Task RenderSection(OpenTagToken openTag, StreamWriter writer);

        public virtual IProcessor MakeChild(IEnumerable<IToken> tokens, RenderContext renderContext)
        {
            throw new NotSupportedException($"The processor {this.GetType()} does not support child processors.");
        }
    }
}
