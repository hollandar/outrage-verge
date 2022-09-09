using Outrage.TokenParser;
using System.Text.RegularExpressions;

namespace Outrage.Verge.Parser.Tokens;

public class AttributeToken : IToken
{
    public AttributeToken(IEnumerable<IToken> tokens)
    {
        this.Name = tokens.OfType<AttributeNameToken>().Single();
        this.Value = tokens.OfType<AttributeValueToken>().FirstOrDefault();
    }

    public AttributeToken(string tag, string? value = null)
    {
        this.Name = new AttributeNameToken(tag);
        if (value != null)
            this.Value = new AttributeValueToken(value);
    }

    public AttributeNameToken Name { get; set; }
    public AttributeValueToken? Value { get; set; }

    public string AttributeName => Name?.Name?.Name ?? String.Empty;
    public string AttributeValue => Value?.Value ?? String.Empty;

    public void SetValue(string? value)
    {
        this.Value = new AttributeValueToken(value);
    }

    public override string ToString()
    {
        if (Value != null)
            return $"{Name.Name.Name}=\"{Value.Value}\"";
        else
            return $"{Name.Name.Name}";
    }
}




