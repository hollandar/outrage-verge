using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Outrage.Verge.Search.Models
{
    internal class IndexDocument
    {
        public IndexDocument(IEnumerable<WordOccurrences> words)
        {
            Words = words;
        }

        [JsonPropertyName("words")]
        public IEnumerable<WordOccurrences> Words { get; set; }
    }
}
