using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Outrage.Verge.Search.Models
{
    public class DocumentsDocument
    {
        public DocumentsDocument(IEnumerable<Document> documents)
        {
            this.Documents = documents;
        }

        [JsonPropertyName("documents")]
        public IEnumerable<Document> Documents { get; set; }
    }
}
