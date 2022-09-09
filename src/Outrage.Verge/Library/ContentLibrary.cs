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
    public class ContentLibrary
    {
        static readonly Regex frontmatterRegex = new Regex("^(?:(?<frontmatter>.*?)--){0,1}(?<content>.*)$", RegexOptions.Singleline | RegexOptions.Compiled);

        PathBuilder rootPath;

        Dictionary<string, IEnumerable<IToken>> tokenCache = new();
        Dictionary<string, (string frontmatter, string content)> contentCache = new();

        public ContentLibrary (string rootPath)
        {
            this.rootPath = PathBuilder.From(rootPath);
            if (!this.rootPath.IsDirectory)
            {
                throw new ArgumentException($"The content library {rootPath} is expected to be a folder.");
            }
        }

        public IEnumerable<ContentName> ListContent(string globPattern, ContentName? contentFolder = null)
        {
            var contentDirectory = this.rootPath / contentFolder;

            var contentFiles = Glob.Files(contentDirectory, globPattern);
            foreach (var contentFile in contentFiles)
            {
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

        protected (string frontmatter, string content) GetContentAndFrontmatter(ContentName contentName)
        {
            var contentKey = contentName.Standardized;
            if (contentCache.ContainsKey(contentKey))
            {
                return contentCache[contentKey];
            }

            var content = LoadContent(contentName);
            var contentMatch = frontmatterRegex.Match(content);
            if (contentMatch.Success)
            {
                var frontmatterSection = contentMatch.Groups["frontmatter"].Success ? contentMatch.Groups["frontmatter"].Value : String.Empty;
                var contentSection = contentMatch.Groups["content"].Success ? contentMatch.Groups["content"].Value : String.Empty;

                contentCache[contentKey] = (frontmatterSection, contentSection);
                return (frontmatterSection, contentSection);
            }

            throw new ArgumentException($"Could not retrieve content/frontmatter from {contentName}.");
        }

        public string GetFrontmatterString(ContentName contentName)
        {
            return GetContentAndFrontmatter(contentName).frontmatter;
        }

        public TType GetFrontmatter<TType>(ContentName contentName) where TType: new()
        {
            var frontmatterString = GetFrontmatterString(contentName);
            var yamlDeserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

            var frontmatter = yamlDeserializer.Deserialize<TType>(frontmatterString) ?? new TType();

            return frontmatter;
        }

        public string GetContentString(ContentName contentName)
        {
            return GetContentAndFrontmatter(contentName).content;
        }

        public IEnumerable<IToken> GetHtml(ContentName filename)
        {
            var lowerFilename = filename.Standardized;
            if (tokenCache.ContainsKey(lowerFilename))
            {
                return tokenCache[lowerFilename];
            }

            var content = GetContentString(filename);
            var tokens = HTMLParser.Parse(content);
            this.tokenCache[lowerFilename] = tokens;

            return tokens;

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

            if (path.IsFile)
            {
                return Compose.Serialize.Serializer.Deserialize<TType>(path);
            } else
            {
                var yamlPath = this.rootPath / $"{filename}.yaml";
                if (yamlPath.IsFile)
                {
                    return Compose.Serialize.Serializer.Deserialize<TType>(yamlPath);
                } else
                {
                    var jsonPath = this.rootPath / $"{filename}.json";
                    if (jsonPath.IsFile)
                    {
                        return Compose.Serialize.Serializer.Deserialize<TType>(jsonPath);
                    }
                    else
                        throw new ArgumentException($"{filename} does not exist, yaml and json extensions were also tried.");
                }
            }
        }
    }
}
