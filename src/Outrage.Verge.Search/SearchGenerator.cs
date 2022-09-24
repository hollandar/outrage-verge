using Microsoft.Extensions.Logging;
using Outrage.TokenParser;
using Outrage.TokenParser.Tokens;
using Outrage.Verge.Extensions;
using Outrage.Verge.Library;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor;
using Outrage.Verge.Search.Models;
using System.Diagnostics;
using System.Text;

namespace Outrage.Verge.Search
{
    public class SearchGenerator : IContentGenerator
    {
        private const bool parallelIndexing = false;
        private static HashSet<char> spaceEquivalent = new HashSet<char> { ' ', '\t', '/', '\n', '\r', '~', '`', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '_', '+', '=', '{', '[', '}', ']', ':', ';', '"', '<', ',', '>', '.', '?', '/' };
        private static HashSet<char> punctuation = new HashSet<char> { '\'', ',' };
        private static HashSet<string> inlineTags = new HashSet<string> { "span", "i", "strong", "em" };
        private readonly List<(string contentUri, ContentName publishName)> publishItems = new();

        public Task ContentUpdated(RenderContext renderContext, string contentUri, ContentName publishName)
        {
            publishItems.Add((contentUri, publishName));

            return Task.CompletedTask;
        }

        public async Task Finalize(RenderContext renderContext)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            IDictionary<string, WordOccurrences> wordOccurrences = new Dictionary<string, WordOccurrences>();

            List<Document> documents = new List<Document>();
            List<Task<DocumentModel>> indexTasks = new();
            {
                int documentIndex = 0;
                foreach (var item in publishItems)
                {
                    var thisDocumentIndex = documentIndex++;
                    var indexTask = IndexDocument(renderContext, thisDocumentIndex, item.publishName);
                    if (!parallelIndexing) await indexTask;
                    indexTasks.Add(indexTask);
                    documents.Add(new Document(thisDocumentIndex, item.contentUri));
                }

            }

            Task.WaitAll(indexTasks.ToArray());

            foreach (var task in indexTasks)
            {
                var index = task.Result;
                foreach (var word in index.Words)
                {
                    if (wordOccurrences.TryGetValue(word.Word, out var occurrence))
                    {
                        occurrence.AddOccurences(index.DocumentIndex, word.Occurrences);
                    }
                    else
                    {
                        wordOccurrences[word.Word] = new WordOccurrences(word.Word, index.DocumentIndex, word.Occurrences);
                    }
                }

                var document = documents.Where(doc => doc.DocumentIndex == index.DocumentIndex).Single();
                document.Title = index.Title ?? document.Uri;
                document.Description = index.Description ?? String.Empty;
            }

            var documentsDocument = new DocumentsDocument(documents);
            var documentFilename = new ContentName($"static/index/documents.json");
            using var documentWriter = renderContext.PublishLibrary.OpenWriter(documentFilename);
            var documentsContent = System.Text.Json.JsonSerializer.Serialize(documentsDocument);
            documentWriter.Write(documentsContent);


            var groupedWordOccurrences = wordOccurrences.Values.GroupBy(occ => occ.Word.First(), occ => occ);
            var indexDocuments = groupedWordOccurrences.Select(o => (o.Key, new IndexDocument(o)));
            foreach (var indexDocument in indexDocuments)
            {
                var filename = new ContentName($"static/index/{indexDocument.Key}.json");
                using var writer = renderContext.PublishLibrary.OpenWriter(filename);
                var indexContent = System.Text.Json.JsonSerializer.Serialize(indexDocument.Item2);
                writer.Write(indexContent);
            }

            stopwatch.Stop();
            renderContext.LogInformation($"Document index created id {stopwatch.ElapsedMilliseconds}");
        }


        private async Task<DocumentModel> IndexDocument(RenderContext renderContext, int documentIndex, ContentName publishName)
        {
            try
            {
                var tokens = await TokenizeStream(renderContext, publishName);
                return await Wordify(documentIndex, tokens);
            }
            catch (Exception e)
            {
                renderContext.LogError($"{e.Message} while indexing {publishName}");
                throw e;
            }

        }

        private async Task<IEnumerable<IToken>> TokenizeStream(RenderContext context, ContentName publishName)
        {
            var content = await context.PublishLibrary.LoadContentAsync(publishName);
            var tokens = Outrage.Verge.Parser.HTMLParser.Parse(content);

            return tokens;
        }

        private Task<DocumentModel> Wordify(int documentIndex, IEnumerable<IToken> tokens)
        {
            var documentModel = new DocumentModel(documentIndex);

            StringBuilder wordBuilder = new StringBuilder();
            var headerTokens = tokens.GetInnerTokens("head").ToList();
            var bodyTokens = tokens.GetInnerTokens("body").ToList();
            var words = new Dictionary<string, int>();

            if (!bodyTokens.Any())
            {
                return Task.FromResult(documentModel);
            }

            documentModel.Title = headerTokens.GetTextValue("title");
            documentModel.Description = headerTokens.GetTextValue("meta", token => token.HasAttribute("type") && token.GetAttributeValue("type") == "description");

            foreach (var bodyToken in bodyTokens)
            {
                var progressWord = () =>
                {
                    if (wordBuilder.Length > 0)
                    {
                        var word = wordBuilder.ToString().ToLower();
                        if (!String.IsNullOrWhiteSpace(word))
                        {
                            if (words.ContainsKey(word))
                                words[word] = words[word] + 1;
                            else
                                words[word] = 1;
                        }
                        else System.Diagnostics.Debugger.Break();
                    }
                    wordBuilder.Clear();

                };

                if (bodyToken is OpenTagToken && inlineTags.TryGetValue(((OpenTagToken)bodyToken).NodeName, out var _)) continue;
                if (bodyToken is CloseTagToken && inlineTags.TryGetValue(((CloseTagToken)bodyToken).NodeName, out var _)) continue;

                if (!(bodyToken is StringValueToken))
                {
                    progressWord();
                    continue;
                }

                var stringValueToken = bodyToken as StringValueToken;
                foreach (var character in stringValueToken!.Value.ToArray())
                {
                    if (spaceEquivalent.Contains(character))
                        progressWord();
                    else if (!punctuation.Contains(character))
                        wordBuilder.Append(character);
                }

            }

            documentModel.Words = words.Select(r => new WordModel(r.Key, r.Value));
            return Task.FromResult(documentModel);
        }

        public void Reset()
        {
            publishItems.Clear();
        }
    }
}