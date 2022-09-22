using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Outrage.Verge.Search.Models
{
    public class DocumentOccurrence
    {
        public DocumentOccurrence(int document, int occurrences)
        {
            Document = document;
            Occurrences = occurrences;
        }

        [JsonPropertyName("d")]
        public int Document { get; set; }
        [JsonPropertyName("o")]
        public int Occurrences { get; set; }
    }
}
