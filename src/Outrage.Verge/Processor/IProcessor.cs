using Outrage.Verge.Parser.Tokens;

namespace Outrage.Verge.Processor
{
    public interface IProcessor
    {
        void RenderToStream(Stream stream);
        void RenderToStream(StreamWriter stream);
        void RenderSection(OpenTagToken openTag, StreamWriter writer);
    }
}
