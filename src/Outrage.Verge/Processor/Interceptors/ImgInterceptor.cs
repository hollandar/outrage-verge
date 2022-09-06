using Outrage.TokenParser;
using Outrage.Verge.Extensions;
using Outrage.Verge.Library;
using Outrage.Verge.Parser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Interceptors
{
    public class PictureInterceptor : IInterceptor
    {
        public string GetTag()
        {
            return "Picture";
        }

        public async Task<IEnumerable<IToken>?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var src = openTag.GetAttributeValue<string>("src");
            var sizes = new Size[] {
                new Size (720),
                new Size (1024),
                new Size(1080),
                new Size(1440),
                new Size(1920),
                new Size(2160),
                new Size(2560),
                new Size(3840),
                new Size(5120),
                new Size(7680)
            };
            if (openTag.HasAttribute("sizes"))
            {
                var sizesString = openTag.GetAttributeValue<string>("sizes");
                sizes = sizesString.FromSeparatedValues<int>().Select(w => new Size(w)).ToArray();
            }

            var outputSizes = await renderContext.PublishLibrary.Resize(src, sizes);

            List<IToken> resultTokens = new List<IToken>();
            resultTokens.Add(new OpenTagToken("picture"));
            var imageName = ContentName.From(src);
            var srcSetItems = outputSizes.Select(size => (imageName.InjectExtension($"w{size.width}").ToUri(), $"{size.width}w"))
                .Select(image => $"{image.Item1} {image.Item2}");
            var sourceTag = new OpenTagToken("source",
                new AttributeToken("srcset", String.Join(", ", srcSetItems))
            );
            resultTokens.Add(sourceTag);
            var imgTag = new OpenTagToken("img",
                new AttributeToken("src", src),
                new AttributeToken("style", "width: auto"),
                openTag.GetAttribute("alt")
            );
            resultTokens.Add(imgTag);
            resultTokens.Add(new CloseTagToken("picture"));

            return resultTokens;
        }
    }
}
