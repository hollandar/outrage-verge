using Outrage.TokenParser;

namespace Outrage.Verge.Parser.Tokens;

public class HtmlIdentifierToken : IToken
{
    public HtmlIdentifierToken(string name)
    {
        this.Name = name;
    }

    public string Name { get; set; }
}




