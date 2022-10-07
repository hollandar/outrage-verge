using Outrage.TokenParser;
using Outrage.Verge.Extensions;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor.Html;

namespace Outrage.Verge.Processor.Interceptors;

public class OnLinkInterceptor : IInterceptor
{
    public bool CanHandle(RenderContext renderContext, string name)
    {
        return name == "OnLink";
    }

    public async Task<InterceptorResult?> RenderAsync(HtmlProcessor parentProcessor, RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
    {
        var uri = openTag.AssertAttributeValue<string?>("uri", "Uri is required on the OnLink tag.")?.ReplaceVariables(renderContext.Variables);
        var pageUri = renderContext.Variables.GetValue<string>("uri");
        if (String.IsNullOrWhiteSpace(pageUri))
            throw new ArgumentException("The page uri is not known, should be defined in the uri variable.");

        var variables = Variables.Empty;
        if (uri == pageUri)
            foreach (var attribute in openTag.Attributes)
            {
                variables.SetValue($"{attribute.AttributeName}", attribute.AttributeValue);
            }

        variables.SetValue("uri", uri);

        var childRenderContext = renderContext.CreateChildContext(null, variables);
        var htmlProcessor = parentProcessor.MakeChild(tokens, childRenderContext);
        await htmlProcessor.RenderToStream(writer);

        return null;
    }
}