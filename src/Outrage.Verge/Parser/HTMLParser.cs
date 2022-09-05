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
        .Convert((string value) => new HtmlIdentifierToken(value));
    
    public static IMatcher Variable =
            Matcher.Char('$').Ignore()
                .Then(Matcher.Char('(').Ignore())
                .Then(Identifiers.Identifier.DelimitedBy(Characters.Period)).Wrap(match => new VariableToken(match.Tokens))
                .Then(Matcher.Char(')').Ignore());

    public static IMatcher AttributeNameOnly = Identifier.Wrap((match) => new AttributeNameToken(match.Tokens))
        .Wrap((match) => new AttributeToken(match.Tokens));

    public static IMatcher AttributeNameValue = Identifier.Wrap((match) => new AttributeNameToken(match.Tokens))
        .Then(Characters.Equal.Ignore())
        .Then(Matcher.FirstOf(
            Matcher.Surrounded(Characters.AnyChar.Except(Matcher.Char('\'')).Many(), Matcher.Char('\'').Ignore(), Matcher.Char('\'').Ignore()),
            Matcher.Surrounded(Characters.AnyChar.Except(Matcher.Char('"')).Many(), Matcher.Char('"').Ignore(), Matcher.Char('"').Ignore()),
            Characters.AnyChar.Except(Matcher.Chars(' ', '>')).Many()
            ).Convert((string value) => new AttributeValueToken(value))
        ).Wrap((match) => new AttributeToken(match.Tokens));

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
        .Wrap((match) => new OpenTagToken(match.Tokens));

    public static IMatcher CloseTag = Characters.LessThan
        .Then(Characters.ForwardSlash)
        .Then(Identifier)
        .Then(Characters.Whitespaces.Optional().Ignore())
        .Then(Characters.GreaterThan)
        .Wrap((match) => new CloseTagToken(match.Tokens));

    public static IMatcher Script = OpenTag.When(tokens =>
        tokens.OfType<OpenTagToken>().Single().Name.Name == "script"
    ).Then(Matcher.Many(Characters.AnyChar.Except(Matcher.String("</script>")))).Then(CloseTag).When(tokens =>
    {
        return tokens.OfType<CloseTagToken>().Single().NodeName == "script";
    });

    public static IMatcher Code = OpenTag.When(tokens =>
        tokens.OfType<OpenTagToken>().Single().Name.Name == "Code"
    ).Then(Matcher.Many(Characters.AnyChar.Except(Matcher.String("</Code>")))).Then(CloseTag).When(tokens =>
    {
        return tokens.OfType<CloseTagToken>().Single().NodeName == "Code";
    });

    public static IMatcher Content =
        Matcher.Many(
            Matcher.FirstOf(
            Script,
            Code,
            OpenTag,
            CloseTag,
            Variable,
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




