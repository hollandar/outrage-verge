using Markdig.Helpers;
using Outrage.TokenParser;
using Outrage.TokenParser.Matchers;
using Outrage.TokenParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class Variables
    {
        private readonly Dictionary<string, string> values = new();

        public Variables(IDictionary<string, string> variables)
        {
            foreach (var variable in variables)
            {
                values[variable.Key] = variable.Value;
            }
        }

        public Variables(params (string key, string value)[] variables)
        {
            foreach (var variable in variables)
            {
                values[variable.key] = variable.value;
            }
        }

        public Variables(params KeyValuePair<string, string>[] variables)
        {
            foreach (var variable in variables)
            {
                values[variable.Key] = variable.Value;
            }
        }

        public Variables Combine(Variables variables)
        {
            var result = new Variables(this.values);
            foreach (var variable in variables.values)
            {
                result.values.TryAdd(variable.Key, variable.Value);
            }

            return result;
        }

        public bool HasValue(string name)
        {
            return this.values.ContainsKey(name);
        }

        public string GetValue(string name)
        {
            if (this.values.TryGetValue(name, out var result))
                return result;
            else
                throw new ArgumentException($"Variable {name} is undefined.");
        }

        static IMatcher variableMatcher = Matcher.FirstOf(
            Matcher.Char('$').Ignore()
                .Then(Matcher.Char('(').Ignore())
                .Then(Identifiers.Identifier)
                .Then(Matcher.Char(')').Ignore()),
            Characters.AnyChar
        ).Many().Then(Controls.EndOfFile);


        public string ReplaceVariables(string input, string leadin = "$(", string trailin = ")")
        {
            var match = TokenParser.TokenParser.Parse(input, variableMatcher);
            if (match.Success)
            {
                StringBuilder resultBuilder = new StringBuilder();
                foreach (var token in match.Tokens)
                {
                    if (token is IdentifierToken)
                    {
                        var identifierToken = token as IdentifierToken;
                        if (HasValue(identifierToken.Value))
                        {
                            resultBuilder.Append(GetValue(identifierToken.Value));
                        }
                    }
                    if (token is StringValueToken)
                    {
                        var stringValueToken = token as StringValueToken;
                        resultBuilder.Append(stringValueToken.Value);
                    }
                }

                return resultBuilder.ToString();
            }
            else
            {
                throw new ArgumentException(match.Error);
            }
        }
    }
}
