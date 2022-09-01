using Outrage.TokenParser;
using System.Text.RegularExpressions;

namespace Outrage.Verge.Parser.Tokens;

public class CloseTagToken : IToken
{
    public CloseTagToken(IEnumerable<IToken> tokens)
    {
        this.Name = tokens.OfType<HtmlIdentifierToken>().Single();
    }

    public CloseTagToken(string tag)
    {
        this.Name = new HtmlIdentifierToken(tag);
    }

    public CloseTagToken() { }

    public HtmlIdentifierToken Name { get; set; }

    public string NodeName => Name?.Name ?? String.Empty;
    public override string ToString()
    {
        return $"</{Name.Name}>";
    }
}




