using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class TokenEnumerator : IEnumerator<IToken>
    {
        private readonly IEnumerator<IToken> enumerator;

        public TokenEnumerator(IEnumerable<IToken> enumerable)
        {
            this.enumerator = enumerable.GetEnumerator();
        }

        public IToken Current => enumerator.Current;

        object IEnumerator.Current => this.Current!;

        public void Dispose()
        {
            this.enumerator.Dispose();
        }

        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        public void Reset()
        {
            this.enumerator.Reset();
        }

        public IEnumerable<IToken> TakeUntil<TType>(Func<TType?, bool> untilFunc) where TType: IToken
        {
            Stack<string> passedTokens = new();
            while (this.enumerator.MoveNext())
            {
                if (this.enumerator.Current is OpenTagToken)
                {
                    var openTag = (OpenTagToken)this.enumerator.Current;
                    if (!openTag.Closed)
                        passedTokens.Push(openTag.NodeName);
                }
                else if (this.enumerator.Current is CloseTagToken)
                {
                    var closeTagToken = (CloseTagToken)this.enumerator.Current;
                    while (passedTokens.Count > 0 && passedTokens.Peek() != closeTagToken.NodeName)
                        passedTokens.Pop();
                    if (passedTokens.Count > 0 && passedTokens.Peek() == closeTagToken.NodeName)
                        passedTokens.Pop();
                }

                if (passedTokens.Count == 0)
                {
                    if (this.enumerator.Current is TType && untilFunc((TType)this.enumerator.Current))
                    {
                        break; 
                    }
                }

                yield return this.enumerator.Current;
            }
        }
    }
}
