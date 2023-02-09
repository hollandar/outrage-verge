using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    public sealed class ThemeConfiguration:BaseConfiguration
    {
        public string? Name { get; set; }
        public string Template { get; set; } = "theme.t.html";
        public Exec? Exec { get; set; }
        public ICollection<VariableItem> Variables {get;set;} = Array.Empty<VariableItem>();

    }
}
