using Outrage.TokenParser;

namespace Outrage.Verge.Parser.Tokens;

public class AttributeNameToken : IToken
{
    public AttributeNameToken(IEnumerable<IToken> tokens)
    {
        Name = tokens.OfType<HtmlIdentifierToken>().Single();
    }

    public AttributeNameToken(string name)
    {
        this.Name = new HtmlIdentifierToken(name);
    }

    public HtmlIdentifierToken Name { get; set; }
}




