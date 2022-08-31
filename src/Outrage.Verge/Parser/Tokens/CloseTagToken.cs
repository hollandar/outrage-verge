using Outrage.TokenParser;

namespace Outrage.Verge.Parser.Tokens;

public class CloseTagToken : IToken
{
    public HtmlIdentifierToken Name { get; set; }

    public string NodeName => Name?.Name ?? String.Empty;
    public override string ToString()
    {
        return $"</{Name.Name}>";
    }
}




