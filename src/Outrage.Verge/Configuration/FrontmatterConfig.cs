using Outrage.Verge.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    public class FrontmatterBase
    {
        public FrontmatterMeta? Meta { get; set; } = null;

        public virtual void Apply(Variables variables)
        {
            Meta?.Apply(variables);
        }
    }

    public class FrontmatterMeta
    {
        public string Description { get; set; } = string.Empty;
        public string[] Keywords { get; set; } = new string[0];

        public void Apply(Variables variables)
        {
            if (!String.IsNullOrWhiteSpace(this.Description))
                variables.SetValue("meta_description", this.Description);
            if (this.Keywords.Length > 0)
            {
                variables.SetValue("meta_keywords", String.Join("; ", this.Keywords));
            }
        }
    }

}
