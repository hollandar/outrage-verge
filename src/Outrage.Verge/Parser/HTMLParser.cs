﻿using Outrage.Verge.Parser.Tokens;
using Outrage.TokenParser;
using Outrage.TokenParser.Matchers;
using System.Text;
using Outrage.TokenParser.Tokens;

namespace Outrage.Verge.Parser;

public static class HTMLParser
{
    public static IMatcher Identifier =
        Matcher.Some(
            Characters.AnyChar.Except(Matcher.Chars('\t', '\r', '\n', '\f', '>', '/', '\'', '"', '=', ' ')))
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

    public static IMatcher InTagWhitespace = Matcher.FirstOf(
        Characters.Whitespace,
        Controls.EndOfLine)
        .Many();

    public static IMatcher OpenTag = Characters.LessThan
        .Then(Identifier)
        .Then(InTagWhitespace.Optional().Ignore())
        .Then(Matcher.DelimitedBy(Attribute, InTagWhitespace.Ignore()))
        .Then(InTagWhitespace.Optional().Ignore())
        .Then(Characters.ForwardSlash.Optional().Produce<ContainedCloseTagToken>())
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

    public static IMatcher Template = OpenTag.When(tokens =>
        tokens.OfType<OpenTagToken>().Single().Name.Name == "template"
    ).Then(Matcher.Many(Characters.AnyChar.Except(Matcher.String("</template>")))).Then(CloseTag).When(tokens =>
    {
        return tokens.OfType<CloseTagToken>().Single().NodeName == "template";
    });

    public static IMatcher Code = OpenTag.When(tokens =>
        tokens.OfType<OpenTagToken>().Single().Name.Name == "Code"
    ).Then(Matcher.Many(Characters.AnyChar.Except(Matcher.String("</Code>")))).Then(CloseTag).When(tokens =>
    {
        return tokens.OfType<CloseTagToken>().Single().NodeName == "Code";
    });

    public static IMatcher Encoded =
        Characters.Ampersand.Ignore()
        .Then(Characters.AnyChar.Except(Characters.Semicolon).Some().Text())
        .Then(Characters.Semicolon.Ignore())
        .Wrap(match => new EntityToken(match.Tokens.Cast<TextToken>().Single().Value));

    public static IMatcher Content =
        Matcher.Many(
            Matcher.FirstOf(
            Script,
            Template,
            Code,
            OpenTag,
            CloseTag,
            Variable,
            Encoded,
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




