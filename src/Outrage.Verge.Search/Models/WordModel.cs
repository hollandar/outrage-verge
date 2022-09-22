using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Search.Models
{
    public class DocumentModel
    {
        public DocumentModel(int documentIndex)
        {
            this.DocumentIndex = documentIndex;
        }

        public int DocumentIndex { get; set; }
        public string? Title { get; set; } = String.Empty;
        public string? Description { get; set; } = String.Empty;
        public IEnumerable<WordModel> Words { get; set; } = Enumerable.Empty<WordModel>();
    }

    public class WordModel
    {
        public WordModel(string word, int occurrences)
        {
            Word = word;
            Occurrences = occurrences;
        }

        public string Word { get; }
        public int Occurrences { get; }
    }
}
