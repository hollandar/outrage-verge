using Outrage.TokenParser;

namespace Outrage.Verge.Parser.Tokens;

public class AttributeToken : IToken
{
    public AttributeNameToken Name { get; set; }
    public AttributeValueToken? Value { get; set; }

    public string AttributeName => Name?.Name?.Name ?? String.Empty;
    public string AttributeValue => Value?.Value ?? String.Empty;
    public override string ToString()
    {
        if (Value != null)
            return $"{Name.Name.Name}=\"{Value.Value}\"";
        else
            return $"{Name.Name.Name}";
    }
}




