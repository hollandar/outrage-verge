using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    public class SiteConfiguration:BaseConfiguration
    {
        public string? UriName { get; set; }
        public string? Theme { get; set; }
        public ICollection<string> PageGlobs { get; set; } = new string[] { "**/*.html", "**/*.md" };
        public ICollection<string> ExcludeGlobs { get; set; } = new string[] { "node_modules/**/*", "**/*.t.*", "**/*.c.*" };
        public ICollection<VariableItem> Variables { get; set; } = new List<VariableItem>();
        public ICollection<string> Derived { get; set; } = new List<string>();
        public Exec? Exec { get; set; }
        public string Language { get; set; } = "en";
    }

    

    public class VariableItem {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

}
