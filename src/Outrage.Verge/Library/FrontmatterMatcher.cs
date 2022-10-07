using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Library
{
    public struct FrontmatterContainer
    {
        public bool Success;
        public string Frontmatter;
        public string Content;

        public FrontmatterContainer()
        {
            this.Success = false;
            this.Frontmatter = String.Empty;
            this.Content = String.Empty;
        }
    }

    public static class FrontmatterMatcher
    {
        public static FrontmatterContainer Match(string input)
        {
            var frontmatterContainer = new FrontmatterContainer() { Success = false };
            var sourceMemory = input.AsMemory().Span;

            // if there is no hyphen hyphen, there cant be frontmatter.
            if (sourceMemory.IndexOf("--") == -1)
            {
                frontmatterContainer.Success = true;
                frontmatterContainer.Content = input;
                return frontmatterContainer;
            }

            var index = 0;
            var endIndex = -1;
            var startIndex = -1;
            var consideredLength = sourceMemory.Length - 1;
            while (index < consideredLength && endIndex == -1)
            {
                var ix = -1;
                if (sourceMemory[index] == '\n')
                {
                    ix = index + 1;
                }
                if (sourceMemory[index] == '\r' && sourceMemory[index + 1] == '\n')
                {
                    ix = index + 2;
                }
                if (ix == -1)
                {
                    index += ix == -1 ? 1 : (ix - index);
                    continue;
                }
                var iix = ix;
                while (iix < consideredLength && sourceMemory[iix] == '-') iix++;
                if (iix - ix < 2)
                {
                    index = ix;
                    continue;
                }
                    startIndex = ix;
                if (sourceMemory[iix] == '\n')
                {
                    endIndex = iix + 1;
                }
                if (sourceMemory[iix] == '\r' && sourceMemory[iix + 1] == '\n')
                {
                    endIndex = iix + 2;
                }

                index = iix;
            }

            string frontmatter = string.Empty;
            if (startIndex > 0)
            {
                frontmatterContainer.Frontmatter = sourceMemory.Slice(0, startIndex).ToString();
            }

            string content = string.Empty;
            if (endIndex == -1)
            {
                frontmatterContainer.Content = sourceMemory.ToString();
            }
            else if (endIndex < sourceMemory.Length)
            {
                frontmatterContainer.Content = sourceMemory.Slice(endIndex, sourceMemory.Length - endIndex).ToString();
            }

            frontmatterContainer.Success = true;
            return frontmatterContainer;
        }
    }
}
