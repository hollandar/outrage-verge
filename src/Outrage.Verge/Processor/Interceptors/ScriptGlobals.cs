using Outrage.TokenParser;

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
}
