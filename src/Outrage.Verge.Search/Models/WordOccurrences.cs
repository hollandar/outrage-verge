using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Outrage.Verge.Search.Models
{
    internal class WordOccurrences
    {
        public WordOccurrences(string word, IEnumerable<DocumentOccurrence>? occurrences = null)
        {
            this.Word = word;
            this.Occurrences = occurrences?.ToList() ?? new();
        }

        public WordOccurrences(string word, int documentIndex, int occurrences)
        {
            this.Word = word;
            this.Occurrences = new List<DocumentOccurrence> { new DocumentOccurrence(documentIndex, occurrences) };
        }

        [JsonPropertyName("w")]
        public string Word { get; set; }

        [JsonPropertyName("oc")]
        public List<DocumentOccurrence> Occurrences { get; set; }

        public void AddOccurences(int document, int occurrences)
        {
            this.Occurrences.Add(new DocumentOccurrence(document, occurrences));
        }
    }
}
