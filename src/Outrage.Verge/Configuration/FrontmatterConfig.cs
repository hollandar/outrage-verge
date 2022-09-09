using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    public class FrontmatterConfig
    {
        public string Title { get; set; } = Constants.DocumentTitleAttDefault;
        public string Template { get; set; } = Constants.MarkdownLayoutAttDefault;
        public string Section { get; set; } = Constants.BodySection;
        public string HeadSection { get; set; } = Constants.HeadSection;
    }
}
