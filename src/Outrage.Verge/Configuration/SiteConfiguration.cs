﻿using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    public class SiteConfiguration
    {
        public string? UriName { get; set; }
        public string? Theme { get; set; }
        public string ThemesPath { get; set; } = "themes";
        public ICollection<CopyItem> Copy { get; set; } = new List<CopyItem>();
        public ICollection<ContentName> PagePaths { get; set; } = new ContentName[] { "content" };
        public ICollection<string> PageGlobs { get; set; } = new string[] { "**/*.html" };
        public ICollection<VariableItem> Variables { get; set; } = new List<VariableItem>();
        public ICollection<string> LocationFallbacks { get; set; } = new string[]
        {
            "$(themeBase)",
            "base"
        };

        public Exec? Exec { get; set; }
        public ICollection<string> Derived { get; set; } = new List<string>();
    }

    public class CopyItem
    {
        public string? From { get; set; }
        public string Glob { get; set; } = "**/*.*";
        public string? To { get; set; }
    }

    public class VariableItem {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    public class Exec
    {
        public ICollection<string>? Install { get; set; } = null;
        public ICollection<string>? Prebuild { get; set; } = null;
        public ICollection<string>? Postbuild { get; set; } = null;
    }
}
