using Outrage.TokenParser;

namespace Outrage.Verge.Parser.Tokens;

public class AttributeValueToken : IToken
{
    public AttributeValueToken(string value)
    {
        this.Value = value;
    }

    public string Value { get; set; }
}




