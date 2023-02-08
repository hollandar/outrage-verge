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

    public async Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
    {
        var uri = openTag.GetAttributeValue<string?>("uri")?.ReplaceVariables(renderContext.Variables);
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Uri is required on the OnLink tag.");

        var pageUri = renderContext.Variables.GetValue<string>("uri");
        if (String.IsNullOrWhiteSpace(pageUri))
            throw new ArgumentException("The page uri is not known, should be defined in the uri variable.");

        var variables = Variables.Empty;
        if (uri == pageUri)
        {
            foreach (var attribute in openTag.Attributes)
            {
                variables.SetValue($"active_{attribute.AttributeName}", attribute.AttributeValue);
            }
        }
        else
        {
            foreach (var attribute in openTag.Attributes)
            {
                variables.SetValue($"inactive_{attribute.AttributeName}", attribute.AttributeValue);
            }
            variables.SetValue("_uri", uri);
        }

        var childRenderContext = renderContext.CreateChildContext(variables);
        var htmlProcessor = new HtmlProcessor(tokens, childRenderContext);
        await htmlProcessor.RenderToStream(writer);

        return null;
    }
}