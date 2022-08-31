using Outrage.Verge.Parser.Tokens;
using Outrage.TokenParser;
using Outrage.TokenParser.Matchers;
using System.Text;

namespace Outrage.Verge.Parser;

public static class HTMLParser
{
    public static IMatcher Identifier =
        Matcher.Some(
            Characters.AnyChar.Except(Matcher.Chars('\t', '\n', '\f', '>', '/', '\'', '"', '=', ' ')))
        .Convert((string value) =>
        {
            return new HtmlIdentifierToken { Name = value };
        });

    public static IMatcher AttributeNameOnly = Identifier.Wrap((match) =>
        new AttributeNameToken { Name = match.Tokens.OfType<HtmlIdentifierToken>().Single() }
        )
        .Wrap((match) => new AttributeToken
        {
            Name = match.Tokens.OfType<AttributeNameToken>().Single(),
            Value = null
        });

    public static IMatcher AttributeNameValue = Identifier.Wrap((match) =>
        new AttributeNameToken { Name = match.Tokens.OfType<HtmlIdentifierToken>().Single() })
        .Then(Characters.Equal.Ignore())
        .Then(Matcher.FirstOf(
            Matcher.Surrounded(Characters.AnyChar.Except(Matcher.Char('\'')).Many(), Matcher.Char('\'').Ignore(), Matcher.Char('\'').Ignore()),
            Matcher.Surrounded(Characters.AnyChar.Except(Matcher.Char('"')).Many(), Matcher.Char('"').Ignore(), Matcher.Char('"').Ignore()),
            Characters.AnyChar.Except(Matcher.Chars(' ', '>')).Many()
            ).Convert((string value) => new AttributeValueToken { Value = value })
        ).Wrap((match) => new AttributeToken
        {
            Name = match.Tokens.OfType<AttributeNameToken>().Single(),
            Value = match.Tokens.OfType<AttributeValueToken>().Single(),
        });

    public static IMatcher Attribute = Matcher.FirstOf(
        AttributeNameValue,
        AttributeNameOnly
    );

    public static IMatcher OpenTag = Characters.LessThan
        .Then(Identifier)
        .Then(Characters.Whitespaces.Optional())
        .Then(Matcher.DelimitedBy(Attribute, Characters.Whitespaces.Ignore()))
        .Then(Characters.Whitespaces.Optional())
        .Then(Characters.ForwardSlash.Optional().Produce<CloseTagToken>())
        .Then(Characters.GreaterThan)
        .Wrap((match) => new OpenTagToken
        {
            Name = match.Tokens.OfType<HtmlIdentifierToken>().Single(),
            Attributes = match.Tokens.OfType<AttributeToken>(),
            Closed = match.Tokens.OfType<CloseTagToken>().Any()
        });

    public static IMatcher CloseTag = Characters.LessThan
        .Then(Characters.ForwardSlash)
        .Then(Identifier)
        .Then(Characters.Whitespaces.Optional().Ignore())
        .Then(Characters.GreaterThan)
        .Wrap((match) => new CloseTagToken
        {
            Name = match.Tokens.OfType<HtmlIdentifierToken>().Single()
        });

    public static IMatcher Script = OpenTag.When(tokens =>
        tokens.OfType<OpenTagToken>().Single().Name.Name == "script"
    ).Then(Matcher.Many(Characters.AnyChar.Except(Matcher.String("</script")))).Then(CloseTag).When(tokens =>
    {
        return tokens.OfType<CloseTagToken>().Single().Name.Name == "script";
    });

    public static IMatcher Content =
        Matcher.Many(
            Matcher.FirstOf(
            Script,
            OpenTag,
            CloseTag,
            Characters.AnyChar
            )
            ).Then(Controls.EndOfFile);

    public static IEnumerable<IToken> Parse(string content)
    {
        var result = Outrage.TokenParser.TokenParser.Parse(content, Content);

        if (!result.Success)
        {
            throw new ParseException($"ERROR: {result.Error}");
        }
        else
        {
            return result.Tokens;
        }
    }
}




