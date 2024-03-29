﻿using Outrage.TokenParser;
using Outrage.TokenParser.Tokens;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Extensions
{
    public static class TokensEnumerableExtensions
    {
        static HashSet<string> selfClosing = new() { "!DOCTYPE", "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };
        public static string? GetTextValue(this IEnumerable<TokenParser.IToken> tokens, string nodeName, Func<OpenTagToken, bool>? match = null)
        {
            var innerText = tokens.GetInnerTokens(nodeName, match);

            if (!innerText.Any())
                return null;

            StringBuilder valueBuilder = new StringBuilder();

            foreach (var token in innerText.Where(r => r is StringValueToken).Cast<StringValueToken>())
            {
                valueBuilder.Append(token.Value);
            }

            return valueBuilder.ToString();
        }

        public static OpenTagToken? GetFirstToken(this IEnumerable<IToken> tokens, string nodeName, Func<OpenTagToken, bool>? match = null)
        {
            foreach (var token in tokens)
            {
                if (token is OpenTagToken && (match == null || match((OpenTagToken)token)))
                {
                    return (OpenTagToken)token;
                }
            }

            return null;
        }

        public static ICollection<TokenParser.IToken> GetInnerTokens(this IEnumerable<IToken> tokens, string nodeName, Func<OpenTagToken, bool>? match = null)
        {
            return EnumerateInnerTokens(tokens, nodeName, match).ToList();
        }

        public static IEnumerable<TokenParser.IToken> EnumerateInnerTokens(this IEnumerable<IToken> tokens, string nodeName, Func<OpenTagToken, bool>? match = null)
        {
            bool yielding = false;
            Stack<string> nodeNames = new();
            foreach (var token in tokens)
            {
                if (token is OpenTagToken)
                {
                    var openTagToken = (OpenTagToken)token;
                    if (!openTagToken.Closed || !selfClosing.Contains(openTagToken.NodeName))
                        nodeNames.Push(openTagToken.NodeName);

                    if (!yielding && nodeNames.Count == 1 && openTagToken.NodeName == nodeName && (match == null || match(openTagToken)))
                    {
                        yielding = true;
                        continue;
                    }

                }

                if (token is CloseTagToken)
                {
                    var closeTagToken = (CloseTagToken)token;

                    if (!nodeNames.Contains(closeTagToken.NodeName))
                        throw new Exception($"{closeTagToken.ToString()} is unblanaced.");
                    while (nodeNames.Count > 0 && nodeNames.Peek() != closeTagToken.NodeName)
                        nodeNames.Pop();
                    nodeNames.Pop();
                    if (nodeNames.Count == 0)
                        yielding = false;
                }

                if (yielding)
                    yield return token;
            }
        }
    }
}

