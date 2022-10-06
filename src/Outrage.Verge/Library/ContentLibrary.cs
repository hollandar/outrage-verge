using Compose.Path;
using Compose.Serialize;
using GlobExpressions;
using Outrage.TokenParser;
using Outrage.TokenParser.Tokens;
using Outrage.Verge.Parser;
using Outrage.Verge.Processor.Markdown;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeTypeResolvers;

namespace Outrage.Verge.Library
{
    public class ContentCache
    {
        public string frontmatter { get; set; }
        public string content { get; set; }
        public object? frontmatterObject { get; set; }
        public ICollection<IToken>? tokens { get; set; }
    }

    public class ContentLibrary
    {
        private readonly PathBuilder rootPath;
        private readonly Dictionary<string, ContentCache> contentCache = new();

        public ContentLibrary (string rootPath)
        {
            this.rootPath = PathBuilder.From(rootPath);
            if (!this.rootPath.IsDirectory)
            {
                throw new ArgumentException($"The content library {rootPath} is expected to be a folder.");
            }
        }

        public PathBuilder RootPath => this.rootPath;
        public IEnumerable<ContentName> ListContent(string globPattern, ContentName? contentFolder = null, string[]? exclude = null)
        {
            var contentDirectory = this.rootPath / contentFolder;

            var contentFiles = Glob.Files(contentDirectory, globPattern);
            foreach (var contentFile in contentFiles)
            {
                if (exclude?.Any(r => Glob.IsMatch(contentFile, r)) ?? false) continue;
                yield return ContentName.From(contentFile);
            }
        }

        public bool ContentExists(ContentName fileName)
        {
            if (contentCache.ContainsKey(fileName.Standardized))
                return true;

            var path = this.rootPath / fileName;

            return path.IsFile;
        }

        public bool FolderExists(ContentName folderName)
        {
            var path = this.rootPath / folderName;

            return path.IsDirectory;
        }

        protected string LoadContent(ContentName contentName)
        {
            var path = this.rootPath / contentName;

            if (!path.IsFile)
                throw new ArgumentException($"Content file {contentName} does not exist.");

            var content = path.ReadToEnd();

            return content;
        }

        protected ContentCache GetContentAndFrontmatter(ContentName contentName)
        {
            var contentKey = contentName.Standardized;
            if (contentCache.ContainsKey(contentKey))
            {
                var cacheItem = contentCache[contentKey];
                return cacheItem;
            }

            var content = LoadContent(contentName);
            var memory = content.AsMemory();
            

            var contentMatch = FrontmatterMatcher.Match(content);
            if (contentMatch.Success)
            {
                var frontmatterSection = contentMatch.Frontmatter;
                var contentSection = contentMatch.Content;

                var cacheItem = new ContentCache { frontmatter = frontmatterSection, content = contentSection };
                contentCache[contentKey] = cacheItem;
                return cacheItem;
            }

            throw new ArgumentException($"Could not retrieve content/frontmatter from {contentName}.");
        }

        public string GetFrontmatterString(ContentName contentName)
        {
            return GetContentAndFrontmatter(contentName).frontmatter;
        }

        public TType GetFrontmatter<TType>(ContentName contentName) where TType: new()
        {
            var frontmatterString = GetContentAndFrontmatter(contentName);

            if (frontmatterString.frontmatterObject is TType)
                return (TType)frontmatterString.frontmatterObject;

            TType frontmatter = DeserializeFrontmatter<TType>(frontmatterString);
            frontmatterString.frontmatterObject = frontmatter;
            return frontmatter;
        }

        private TType DeserializeFrontmatter<TType>(ContentCache frontmatterString) where TType : new()
        {
            var yamlDeserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

            var frontmatter = yamlDeserializer.Deserialize<TType>(frontmatterString.frontmatter) ?? new TType();
            return frontmatter;
        }

        public string GetContentString(ContentName contentName)
        {
            return GetContentAndFrontmatter(contentName).content;
        }

        public IEnumerable<IToken> GetHtml(ContentName filename)
        {
            var lowerFilename = filename.Standardized;
            if (contentCache.ContainsKey(lowerFilename) && contentCache[lowerFilename].tokens != null)
            {
                return contentCache[lowerFilename].tokens!;
            }

            var content = GetContentString(filename);
            var tokens = HTMLParser.Parse(content).ToList();
            var cacheItem = this.contentCache[lowerFilename];
            this.contentCache[lowerFilename].tokens = tokens;

            return tokens;
        }

        public (TType frontmatter, IEnumerable<IToken> tokens) GetFrontmatterAndHtml<TType>(ContentName filename) where TType : new()
        {
            var content = this.GetContentAndFrontmatter(filename);
            if (content.frontmatterObject is TType && content.tokens != null)
            {
                return ((TType)content.frontmatterObject, content.tokens);
            }

            var tokens = HTMLParser.Parse(content.content).ToList();
            var frontmatter = DeserializeFrontmatter<TType>(content);

            content.tokens = tokens;
            content.frontmatterObject = frontmatter;

            return (frontmatter, tokens);
        }

        public (TType frontmatter, string content) GetFrontmatterAndContent<TType>(ContentName filename) where TType : new()
        {
            var content = this.GetContentAndFrontmatter(filename);
            if (content.frontmatterObject is TType)
            {
                return ((TType)content.frontmatterObject, content.content);
            }

            var frontmatter = DeserializeFrontmatter<TType>(content);

            content.frontmatterObject = frontmatter;

            return (frontmatter, content.content);
        }

        public string GetString(ContentName contentName)
        {
            return LoadContent(contentName);
        }
        
        public Stream OpenStream(ContentName filename)
        {
            var path = this.rootPath / filename;

            if (!path.IsFile)
                throw new ArgumentException($"Content file {filename} does not exist.");

            return new FileStream(path, FileMode.Open, FileAccess.Read);

        }

        public TType? Deserialize<TType>(ContentName filename) where TType: new()
        {
            var path = this.rootPath / filename;

            return Compose.Serialize.Serializer.DeserializeExt<TType>(path);
        }


    }
}
