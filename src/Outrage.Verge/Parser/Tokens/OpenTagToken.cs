using Outrage.TokenParser;
using Outrage.Verge.Processor;
using System.Collections.Immutable;
using System.Data.SqlTypes;
using System.Text;

namespace Outrage.Verge.Parser.Tokens;

public class OpenTagToken : IToken
{
    public OpenTagToken(IEnumerable<IToken> tokens)
    {
        this.Name = tokens.OfType<HtmlIdentifierToken>().Single();
        this.Attributes = tokens.OfType<AttributeToken>().ToList();
        this.Closed = tokens.OfType<ContainedCloseTagToken>().Any();
    }

    public OpenTagToken(string tag, bool closed, params AttributeToken?[] attributes)
    {
        this.Name = new HtmlIdentifierToken(tag);
        this.Attributes = attributes.Where(a => a != null).Cast<AttributeToken>().ToList();
        this.Closed = closed;
    }

    public OpenTagToken(string tag, params AttributeToken?[] attributes) : this(tag, false, attributes)
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
        var underlying = Nullable.GetUnderlyingType(typeof(TAs));
        var defaultValue = default(TAs);
        if (!HasAttribute(name) && (underlying != null || defaultValue == null))
        {
            return default(TAs);
        }

        var attribute = this.Attributes?.Where(r => r.AttributeName == name).FirstOrDefault();
        if (attribute == null)
            throw new ArgumentException($"An attribute with name {name} does not exist.");

        var newValue = Convert.ChangeType(attribute.AttributeValue, underlying ?? typeof(TAs));
        return (TAs)newValue;
    }

    public TAs SetAttributeValue<TAs>(string name, TAs value)
    {
        AttributeToken attributeToken;
        if (HasAttribute(name))
            attributeToken = GetAttribute(name)!;
        else
        {
            attributeToken = new AttributeToken(name);
            this.Attributes.Add(attributeToken);
        }

        if (attributeToken != null && value != null)
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
        HashSet<string> handledAttributes = new();
        if (Attributes?.Any() ?? false)
        {
            foreach (var attribute in Attributes)
            {
                if (attribute.AttributeName.StartsWith('@')) continue;
                tagBuilder.Append(" ");
                if (String.IsNullOrWhiteSpace(attribute.AttributeValue))
                {
                    tagBuilder.Append(attribute.AttributeName);
                }
                else
                {
                    tagBuilder.AppendFormat("{0}=\"{1}\"", attribute.AttributeName, variables.ReplaceVariables(attribute.AttributeValue));

                }

                handledAttributes.Add(attribute.AttributeName);
            }

            foreach (var attribute in Attributes)
            {
                if (!attribute.AttributeName.StartsWith('@')) continue;
                if (attribute.AttributeName == "@attributes")
                {
                    var except = new HashSet<string>(attribute.AttributeValue.Split(",").Select(r => r.Trim()));
                    var localVariables = variables.Attributes;
                    foreach (var local in localVariables.Keys)
                    {
                        if (handledAttributes.Contains(local)) continue;
                        tagBuilder.Append(" ");
                        var isExcluded = except.Contains(local);
                        if (!isExcluded)
                        {
                            var variableValue = localVariables.GetValue<string>(local);
                            if (String.IsNullOrWhiteSpace(variableValue))
                            {
                                tagBuilder.Append(local);
                            } else
                            {
                                var value = variables.ReplaceVariables(variableValue);
                                tagBuilder.AppendFormat("{0}=\"{1}\"", local, value);
                            }
                        }
                    }
                }
            }
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




