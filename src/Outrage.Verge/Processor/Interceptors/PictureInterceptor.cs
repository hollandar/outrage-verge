using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var src = openTag.AssertAttributeValue<string>("src", "Picture should specify src, the name of the static image to resize/render.");
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

            if (openTag.HasAttribute("widths"))
            {
                var sizesString = openTag.GetAttributeValue<string>("widths");
                if (!String.IsNullOrWhiteSpace(sizesString))
                {
                    sizes = sizesString.FromSeparatedValues<int>().Select(w => new Size(w)).ToArray();
                }
            }

            if (openTag.HasAttribute("dimensions"))
            {
                var dimensionsString = openTag.GetAttributeValue<string>("dimensions");
                if (!String.IsNullOrWhiteSpace(dimensionsString))
                {
                    var dimensionEntries = dimensionsString.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    HashSet<int?> consideredSizes = new();
                    foreach (var dimensionString in dimensionEntries)
                    {
                        var dimension = dimensionString.Split("/", StringSplitOptions.RemoveEmptyEntries);
                        int? dimensionWidth = null;
                        if (dimension.Length >= 1)
                        {
                            int.TryParse(dimension[0], out var parsedWidth);
                            dimensionWidth = parsedWidth;
                        }
                        double dimensionModifier = 1;
                        if (dimension.Length >= 2)
                        {
                            double.TryParse(dimension[1], out var modifier);
                            dimensionModifier = modifier;
                        }
                        if (dimension.Length > 2)
                        {
                            throw new ArgumentException("Too any dimension modifiers, should be in the format width/scalingFactor, example:1024/0.5,8192/0.25");
                        }

                        foreach (var size in sizes.Where(size => size.width != null))
                        {
                            if (dimension[0] == "*" || size.width == dimensionWidth)
                            {
                                if (!consideredSizes.Contains(size.width))
                                {
                                    consideredSizes.Add(size.width);
                                    size.resizeWidth = (int)Math.Ceiling(size.width!.Value / dimensionModifier);
                                }
                            }
                        }
                    }
                }
            }

            var outputSizes = await renderContext.PublishLibrary.Resize(srcValue, sizes);

            var imageName = ContentName.From(srcValue);
            var srcSetItems = outputSizes.Select(size => (imageName.InjectExtension($"w{size.resizeWidth ?? size.width}").ToUri(), $"{size.width}w"))
                .Select(image => $"{image.Item1} {image.Item2}");

            var variables = new Variables(
                ("srcset", String.Join(", ", srcSetItems)),
                ("src", srcValue),
                ("sizes", openTag.GetAttributeValue<string>("sizes") ?? "")
            );
            var childRenderContext = renderContext.CreateChildContext(openTag.Attributes, variables);
            await childRenderContext.RenderComponent("components/picture.c.html", writer);

            return null;
        }
    }
}
