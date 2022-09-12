using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Outrage.TokenParser;
using Outrage.TokenParser.Tokens;
using Outrage.Verge.Parser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor.Interceptors
{
    public class ScriptGlobals
    {
        public ScriptGlobals(RenderContext renderContext, IDictionary<string, string> parameters)
        {
            this.RenderContext = renderContext;
            this.Params = parameters;
        }

        public RenderContext RenderContext { get; set; }
        public IDictionary<string, string> Params { get; set; }
        public List<IToken> EmitTokens { get; set; } = new List<IToken>();

        public void Emit(params IToken[] tokens)
        {
            EmitTokens.AddRange(tokens);
        }
    }

    public class CSCodeInterceptor : IInterceptor
    {
        static IDictionary<string, Script<object>> compilationCache = new Dictionary<string, Script<object>>();
        static SHA256 sha256 = SHA256.Create();
        public bool CanHandle(RenderContext renderContext, string tagName)
        {
            return tagName == "Code";
        }

        public async Task<InterceptorResult?> RenderAsync(RenderContext renderContext, OpenTagToken openTag, IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var codeBuilder = new StringBuilder();
            foreach (var stringValue in tokens.OfType<StringValueToken>())
                codeBuilder.Append(stringValue.Value);
            var code = codeBuilder.ToString();

            var sha = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
            var shaBase64 = Convert.ToBase64String(sha);

            Script<object>? script = null;
            if (!compilationCache.TryGetValue(shaBase64, out script)) {
                var scriptOptions = ScriptOptions.Default.WithImports(
                        "Outrage.Verge", 
                        "Outrage.Verge.Parser.Tokens", 
                        "Outrage.TokenParser.Tokens", 
                        "System"
                    )
                    .WithReferences(
                        this.GetType().Assembly,
                        typeof(StringValueToken).Assembly
                    );
                script = CSharpScript.Create(codeBuilder.ToString(), scriptOptions, globalsType: typeof(ScriptGlobals));
                compilationCache[shaBase64] = script;
            }

            if (script != null)
            {
                var parameters = openTag.Attributes.ToDictionary(r => r.AttributeName, r => renderContext.Variables.ReplaceVariables(r.AttributeValue));
                var scriptGlobals = new ScriptGlobals(renderContext, parameters);

                var scriptState = await script.RunAsync(scriptGlobals);

                if (scriptState.Exception != null)
                    throw scriptState.Exception;

                if (scriptGlobals.EmitTokens?.Any() ?? false)
                {
                    return new InterceptorResult(scriptGlobals.EmitTokens);
                }
            }
            else
            {
                throw new ArgumentException("There was a problem compiling the script.");
            }


            return null;
        }
    }
}
