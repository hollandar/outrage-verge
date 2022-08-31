using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    internal class ThemeConfiguration
    {
        public string Name { get; set; }
        public string[] Copy { get; set; }
        public string Template { get; set; } = "theme.t.html";

    }
}
