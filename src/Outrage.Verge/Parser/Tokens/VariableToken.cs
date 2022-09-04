using Outrage.TokenParser;
using Outrage.TokenParser.Tokens;
using System.Text;
using System.Text.RegularExpressions;

namespace Outrage.Verge.Parser.Tokens;

public class VariableToken : IToken
{
    public VariableToken(IEnumerable<IToken> tokens)
    {
        this.Names = tokens.OfType<IdentifierToken>().ToList();
    }

    public VariableToken(string periodDelimitedName)
    {
        var splitName = periodDelimitedName.Split('.');

        this.Names = splitName.Select(name => new IdentifierToken(name));
    }

    public IEnumerable<IdentifierToken> Names { get; set; }

    public string VariableName { get { return String.Join('.', this.Names.Select(r => r.Value)); } }

    public override string ToString()
    {
        return $"$({VariableName})";
    }
}




