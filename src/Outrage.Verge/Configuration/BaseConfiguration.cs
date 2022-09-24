using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Configuration
{
    public interface ICopyConfiguration
    {
        ICollection<CopyItem> Copy { get; }
    }
    public class BaseConfiguration : ICopyConfiguration
    {
        public ICollection<CopyItem> Copy { get; set; } = new List<CopyItem>();

    }

    public class CopyItem
    {
        public string? From { get; set; }
        public string Glob { get; set; } = "**/*.*";
        public string? To { get; set; }
    }
}
