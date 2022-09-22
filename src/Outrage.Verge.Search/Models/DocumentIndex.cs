using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Outrage.Verge.Search.Models
{
    public class Document
    {
        public Document(int documentIndex, string uri)
        {
            this.DocumentIndex = documentIndex;
            this.Uri = uri;
        }

        [JsonPropertyName("i")]
        public int DocumentIndex { get; set; }

        [JsonPropertyName("u")]
        public string Uri { get; set; }
        [JsonPropertyName("d")]
        public string Description { get; internal set; }
        [JsonPropertyName("t")]
        public string Title { get; internal set; }
    }
}
