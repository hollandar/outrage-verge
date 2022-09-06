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
        private static Regex folderRegex = new Regex("^(?:(?<folder>.+)/){0,1}(?<filename>.+?)(?:[.](?<extension>[^.]*)|$)", RegexOptions.Compiled);
        string itemName;

        public ContentName(string itemName)
        {
            this.itemName = itemName.TrimStart('/').Replace('\\', '/');
        }

        public string Value => itemName;

        public string Standardized => itemName?.ToLower() ?? String.Empty;
        public string Folder
        {
            get
            {
                var match = folderRegex.Match(this.itemName);
                if (match.Success)
                {
                    return match.Groups["folder"].Value ?? String.Empty;
                }

                throw new ArgumentException($"Item name {itemName} could not be parsed for folder.");
            }
        }

        public ContentName InjectExtension(string extension)
        {
            var match = folderRegex.Match(itemName);
            if (match.Success)
            {
                var builder = new StringBuilder();
                if (match.Groups["folder"].Success)
                    builder.AppendFormat("{0}/", match.Groups["folder"].Value);
                if (match.Groups["filename"].Success)
                    builder.Append(match.Groups["filename"].Value);
                builder.AppendFormat(".{0}", extension);
                if (match.Groups["extension"].Success)
                {
                    builder.AppendFormat(".{0}", match.Groups["extension"].Value);
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

        public static ContentName empty = new ContentName(String.Empty);
        public static ContentName Empty => empty;

        public static ContentName GetContentNameFromRelativePaths(PathBuilder file, PathBuilder path)
        {
            return file.GetRelativeTo(path).Path;
        }
    }
}
