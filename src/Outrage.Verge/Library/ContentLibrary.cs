using Compose.Path;
using Compose.Serialize;
using Outrage.TokenParser;
using Outrage.TokenParser.Tokens;
using Outrage.Verge.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NodeTypeResolvers;

namespace Outrage.Verge.Library
{
    public class ContentLibrary
    {
        PathBuilder rootPath;

        Dictionary<string, IEnumerable<IToken>> htmlCache = new();

        public ContentLibrary (string rootPath)
        {
            this.rootPath = new PathBuilder(rootPath);
            if (!this.rootPath.IsDirectory)
            {
                throw new ArgumentException($"The content library {rootPath} is expected to be a folder.");
            }
        }

        public bool ContentExists(string fileName)
        {
            if (htmlCache.ContainsKey(fileName))
                return true;

            var path = this.rootPath / fileName;

            return path.IsFile;
        }

        public bool FolderExists(string folderName)
        {
            var path = this.rootPath / folderName;

            return path.IsDirectory;
        }

        public IEnumerable<IToken> GetHtml(string filename)
        {
            var lowerFilename = filename.ToLower();
            if (htmlCache.ContainsKey(lowerFilename))
            {
                return htmlCache[lowerFilename];
            }

            var path = this.rootPath / filename;
            if (path.Extension != ".html")
                throw new ArgumentException($"Content file {filename} is not Html.");

            if (!path.IsFile)
                throw new ArgumentException($"Content file {filename} does not exist.");

            var content = path.ReadToEnd();
            var tokens = HTMLParser.Parse(content);
            this.htmlCache[lowerFilename] = tokens;

            return tokens;

        }

        public TType Deserialize<TType>(string filename) where TType: new()
        {
            var path = this.rootPath / filename;

            if (path.IsFile)
            {
                return (TType)Serializer.Deserialize<TType>(path);
            } else
            {
                var yamlPath = this.rootPath / $"{filename}.yaml";
                if (yamlPath.IsFile)
                {
                    return (TType)Serializer.Deserialize<TType>(yamlPath);
                } else
                {
                    var jsonPath = this.rootPath / $"{filename}.json";
                    if (jsonPath.IsFile)
                    {
                        return (TType)Serializer.Deserialize<TType>(jsonPath);
                    }
                    else
                        throw new ArgumentException($"{filename} does not exist, yaml and json extensions were also tried.");
                }
            }
        }
    }
}
