using Outrage.TokenParser;
using System;
using System.Collections.Generic;
using System.Text;

namespace Outrage.Verge.Parser.Tokens
{
    public class EntityToken:IToken
    {
        public EntityToken(string inner)
        {
            this.Value = inner;
        }

        public string Value { get; set; }

        public override string ToString()
        {
            return $"&{Value};";
        }
    }
}
