using Outrage.TokenParser;
using Outrage.Verge.Extensions;
using Outrage.Verge.Library;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Interceptors
{
    
    public class PictureInterceptor : IInterceptor
    {
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "Picture";
        }

        public async Task<InterceptorResult?> RenderAsync(HtmlProcessor parentProcessor, RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var src = openTag.GetAttributeValue<string>("src");
            var srcValue = renderContext.Variables.ReplaceVariables(src);
            var sizes = new Size[] {
                new Size(720),
                new Size(1024),
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

            var outputSizes = await renderContext.PublishLibrary.Resize(srcValue, sizes);

            var imageName = ContentName.From(srcValue);
            var srcSetItems = outputSizes.Select(size => (imageName.InjectExtension($"w{size.width}").ToUri(), $"{size.width}w"))
                .Select(image => $"{image.Item1} {image.Item2}");

            var variables = new Variables(
                ("srcset", String.Join(", ", srcSetItems)),
                ("src", srcValue)
            );
            var childRenderContext = renderContext.CreateChildContext(openTag.Attributes, variables);
            await childRenderContext.RenderComponent("components/picture.c.html", writer);

            return null;
        }
    }
}
