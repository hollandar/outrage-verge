using Outrage.TokenParser;
using Outrage.Verge.Processor;
using System.Collections.Immutable;
using System.Data.SqlTypes;
using System.Text;

namespace Outrage.Verge.Parser.Tokens;

public class OpenTagToken : IToken
{
    public OpenTagToken(IEnumerable<IToken> tokens) {
        this.Name = tokens.OfType<HtmlIdentifierToken>().Single();
        this.Attributes = tokens.OfType<AttributeToken>().ToList();
        this.Closed = tokens.OfType<CloseTagToken>().Any();
    }

    public OpenTagToken(string tag, bool closed, params AttributeToken[] attributes)
    {
        this.Name = new HtmlIdentifierToken(tag);
        this.Attributes = attributes.Where(a => a != null).ToList();
        this.Closed = closed;
    }

    public OpenTagToken(string tag, params AttributeToken?[] attributes): this(tag, false, attributes)
    {

    }

    public HtmlIdentifierToken Name { get; set; }
    public List<AttributeToken> Attributes { get; set; }
    public bool Closed { get; set; } = false;

    public string NodeName => Name?.Name ?? String.Empty;

    public bool HasAttribute(string name)
    {
        return this.Attributes?.Any(r => r.AttributeName == name) ?? false;
    }

    public TAs GetAttributeValue<TAs>(string name)
    {
        var attribute = this.Attributes?.Where(r => r.AttributeName == name).FirstOrDefault();
        if (attribute == null)
            throw new ArgumentException($"An attribute with name {name} does not exist.");

        var underlying = Nullable.GetUnderlyingType(typeof(TAs));
        var newValue = Convert.ChangeType(attribute.AttributeValue, underlying ?? typeof(TAs));
        return (TAs)newValue;
    }

    public TAs SetAttributeValue<TAs>(string name, TAs value)
    {
        AttributeToken attributeToken;
        if (HasAttribute(name))
            attributeToken = GetAttribute(name);
        else
        {
            attributeToken = new AttributeToken(name);
            this.Attributes.Add(attributeToken);
        }

        if (attributeToken != null)
            attributeToken.SetValue(value.ToString());

        return value;
    }

    public AttributeToken? GetAttribute(string name)
    {
        var attribute = this.Attributes?.Where(r => r.AttributeName == name).FirstOrDefault();
        return attribute;
    }

    public string GetAttributeValue(string name)
    {
        return GetAttributeValue<string>(name);
    }

    public string ToAttributedString(Variables variables)
    {
        var tagBuilder = new StringBuilder("<");
        tagBuilder.Append(Name.Name);
        if (Attributes?.Any() ?? false)
        {
            tagBuilder.Append(" ");
            tagBuilder.Append(String.Join(" ", Attributes.Select(r => $"{r.AttributeName}=\"{variables.ReplaceVariables(r.AttributeValue)}\"")));
        }

        if (Closed)
        {
            tagBuilder.Append("/");
        }

        tagBuilder.Append(">");

        return tagBuilder.ToString();
    }

    public override string ToString()
    {
        var tagBuilder = new StringBuilder("<");
        tagBuilder.Append(Name.Name);
        if (Attributes?.Any() ?? false)
        {
            tagBuilder.Append(" ");
            tagBuilder.Append(String.Join(" ", Attributes.Select(r => r.ToString())));
        }

        if (Closed)
        {
            tagBuilder.Append("/");
        }

        tagBuilder.Append(">");

        return tagBuilder.ToString();
    }
}




