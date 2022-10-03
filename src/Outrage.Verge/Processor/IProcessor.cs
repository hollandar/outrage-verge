using Outrage.TokenParser;
using Outrage.Verge.Parser.Tokens;

namespace Outrage.Verge.Processor
{
    public interface IProcessor
    {
        Task RenderToStream(Stream stream);
        Task RenderToStream(StreamWriter stream);
        Task RenderSection(OpenTagToken openTag, StreamWriter writer);
        IProcessor MakeChild(IEnumerable<IToken> tokens, RenderContext renderContext);
    }
}
