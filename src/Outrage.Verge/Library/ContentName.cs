using Compose.Path;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Outrage.Verge.Library
{
    public class ContentName
    {
        private static Regex folderRegex = new Regex("^(?:(?<folder>.+)/){0,1}(?<filename>.+?)(?:[.](?<extension>[^.]+)$|$)", RegexOptions.Compiled);
        private Match folderRegexMatch;
        string itemName;


        public ContentName(string itemName)
        {
            this.itemName = itemName.TrimStart('/').Replace('\\', '/').TrimStart('/').TrimEnd('/');
            folderRegexMatch = folderRegex.Match(this.itemName);
        }

        public string Value => itemName;
        public string Standardized => itemName?.ToLower() ?? String.Empty;
        public string Folder
        {
            get
            {
                if (folderRegexMatch.Success && folderRegexMatch.Groups["Folder"].Success)
                {
                    return folderRegexMatch.Groups["folder"].Value ?? String.Empty;
                }

                throw new ArgumentException($"Item name {itemName} could not be parsed for folder.");
            }
        }

        public string Filename
        {
            get
            {
                if (folderRegexMatch.Success && folderRegexMatch.Groups["filename"].Success)
                {
                    return folderRegexMatch.Groups["filename"].Value + (folderRegexMatch.Groups["extension"].Success? "." + folderRegexMatch.Groups["extension"].Value: String.Empty);
                }

                throw new ArgumentException($"Item name {itemName} could not be parsed for filename.");
            }
        }

        public string FilenameWithoutExtension
        {
            get
            {
                if (folderRegexMatch.Success && folderRegexMatch.Groups["filename"].Success)
                {
                    return folderRegexMatch.Groups["filename"].Value;
                }

                throw new ArgumentException($"Item name {itemName} could not be parsed for filename.");
            }
        }

        public string Extension
        {
            get
            {
                if (folderRegexMatch.Success && folderRegexMatch.Groups["extension"].Success)
                {
                    return "." + folderRegexMatch.Groups["extension"].Value;
                }

                throw new ArgumentException($"Item name {itemName} could not be parsed for extension.");
            }
        }

        public ContentName InjectExtension(string extension)
        {
            if (folderRegexMatch.Success)
            {
                var builder = new StringBuilder();
                if (folderRegexMatch.Groups["folder"].Success)
                    builder.AppendFormat("{0}/", folderRegexMatch.Groups["folder"].Value);
                if (folderRegexMatch.Groups["filename"].Success)
                    builder.Append(folderRegexMatch.Groups["filename"].Value);
                builder.AppendFormat(".{0}", extension);
                if (folderRegexMatch.Groups["extension"].Success)
                {
                    builder.AppendFormat(".{0}", folderRegexMatch.Groups["extension"].Value);
                }

                return new ContentName(builder.ToString());
            }

            throw new ArgumentException($"Item {itemName} could not be parsed.");
        }

        public string ToUri()
        {
            return String.Format("/{0}", this.itemName);
        }
        public static PathBuilder operator /(PathBuilder path, ContentName? item)
        {
            if (item == null) return path;
            return path / item.Value;
        }
        

        public static ContentName operator /(ContentName? path, ContentName? item)
        {
            if (path == null && item != null) return item;
            if (item == null && path != null) return path;
            if (path != null && item != null) return path / item.Value;
            throw new ArgumentException("Both path and item can not be null.");
        }

        public static ContentName operator/(string? path, ContentName? item)
        {
            if (item != null && String.IsNullOrWhiteSpace(path)) return item;
            if (!String.IsNullOrWhiteSpace(path)) return ContentName.From(path) / item;
            throw new ArgumentException("Both path and item can not be null.");
        }

        public static ContentName operator /(ContentName path, string? item)
        {
            if (item == null) return path;
            return $"{path}/{item.TrimEnd('/')}";
        }

        public static implicit operator string(ContentName itemName)
        {
            return itemName.Value;
        }

        public static implicit operator ContentName(string itemName)
        {
            return ContentName.From(itemName);
        }

        public override string ToString()
        {
            return this.itemName;
        }

        public override bool Equals(object? obj)
        {
            var itemName = obj as ContentName;
            if (itemName != null)
            {
                return this.itemName.Equals(itemName.itemName);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return itemName.GetHashCode();
        }

        public static ContentName From(string itemName)
        {
            return new ContentName(itemName);
        }

        private static ContentName empty = new ContentName(String.Empty);
        public static ContentName Empty => empty;

        public static ContentName GetContentNameFromRelativePaths(PathBuilder file, PathBuilder path)
        {
            return file.GetRelativeTo(path).Path;
        }

        public PathBuilder GetPath(PathBuilder rootPath)
        {
            return rootPath / this;
        }
    }
}
