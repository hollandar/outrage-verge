using Outrage.Verge.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Markdown
{
    public class FrontmatterMarkdown : FrontmatterBase
    {
        public string Title { get; set; } = Constants.DocumentTitleAttDefault;
        public string Template { get; set; } = Constants.MarkdownLayoutAttDefault;
        public string Section { get; set; } = Constants.BodySection;
        public string HeadSection { get; set; } = Constants.HeadSection;

        public override void Apply(Variables variables)
        {
            base.Apply(variables);
            variables.SetValue("title", this.Title);
        }
    }
}
