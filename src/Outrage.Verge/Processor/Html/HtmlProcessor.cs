using Outrage.Verge.Parser;
using Outrage.Verge.Parser.Tokens;
using Outrage.TokenParser;
using Outrage.TokenParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Outrage.Verge.Library;
using System.Runtime.CompilerServices;
using Outrage.Verge.Processor.Interceptors;
using Microsoft.Extensions.Logging;

namespace Outrage.Verge.Processor.Html
{
    public class HtmlProcessor : ProcessorBase, IProcessor
    {
        ContentName fallbackContentName;
        private readonly IDictionary<string, List<IToken>> sectionContent = new Dictionary<string, List<IToken>>();
        private IEnumerable<IToken>? tokens;
        private bool skipSpace = true;
        private char lastWritten = char.MinValue;

        public HtmlProcessor(ContentName contentName, IProcessor childPage, RenderContext renderContext) : base(renderContext, childPage)
        {
            Load(contentName);
        }

        public HtmlProcessor(ContentName contentName, RenderContext renderContext) : base(renderContext)
        {
            Load(contentName);
        }

        public HtmlProcessor(IEnumerable<IToken> tokens, RenderContext renderContext) : base(renderContext)
        {
            this.tokens = tokens;
            Process();
        }

        public void Load(ContentName contentName)
        {
            this.fallbackContentName = this.renderContext.GetFallbackContent(contentName);
            tokens = renderContext.ContentLibrary.GetHtml(this.fallbackContentName);

            Process();
        }

        protected void Process()
        {
            ArgumentNullException.ThrowIfNull(tokens);
            var enumerator = new TokenEnumerator(tokens);
            while (enumerator.MoveNext())
            {
                var token = enumerator.Current;
                if (token is OpenTagToken)
                {
                    var openToken = (OpenTagToken)token;
                    if (openToken.NodeName == Constants.DefineSectionTag)
                    {
                        DefineSection(openToken, enumerator.TakeUntil<CloseTagToken>(endSection => endSection?.NodeName == Constants.DefineSectionTag));
                    }
                    else if (openToken.NodeName == Constants.DocumentTag)
                    {
                        if (!openToken.Closed)
                            throw new ArgumentException($"Document tag should be self closing.");
                        SetDocument(openToken);
                    }
                } 
            }

            if (!sectionContent.ContainsKey("body") && this.layoutPage != null)
            {
                this.renderContext.LogWarning("{contentName} did not define a section named body, its entire content was used as body instead.", this.fallbackContentName);
                sectionContent["body"] = tokens.ToList();
            }
        }

        protected void DefineSection(OpenTagToken openToken, IEnumerable<IToken> tokens)
        {
            var sectionName = openToken.GetAttributeValue(Constants.DefineSectionNameAtt);
            var sectionVariable = HandleVariables(sectionName);
            if (!sectionContent.ContainsKey(sectionVariable))
            {
                sectionContent[sectionVariable] = new List<IToken>();
            }

            sectionContent[sectionVariable].AddRange(tokens);
        }

        public override async Task RenderToStream(Stream stream)
        {
            using var writer = new StreamWriter(stream);
            await RenderToStream(writer);
        }

        public override async Task RenderToStream(StreamWriter stream)
        {
            ArgumentNullException.ThrowIfNull(tokens);
            if (layoutPage != null)
                await layoutPage.RenderToStream(stream);
            else
                await RenderContentAsync(tokens, stream);
        }

        protected async Task RenderContentAsync(IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var enumerator = new TokenEnumerator(tokens);
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is StringValueToken)
                {
                    var stringValueToken = (StringValueToken)enumerator.Current;
                    for (var i = 0; i < stringValueToken.Value.Span.Length; i++)
                    {
                        char c = stringValueToken.Value.Span[i];
                        if (c == ' ' && !skipSpace && lastWritten != ' ')
                        {
                            writer.Write(c);
                            lastWritten = c;
                            continue;
                        }


                        if (c != ' ' && c != '\r' && c != '\n')
                        {
                            writer.Write(c);
                            lastWritten = c;
                            skipSpace = false;
                            continue;
                        }
                    }
                    continue;
                }

                if (enumerator.Current is OpenTagToken)
                {
                    var openTagToken = (OpenTagToken)enumerator.Current;

                    if (openTagToken.NodeName == Constants.SectionTag && childPage != null)
                    {
                        var sectionName = openTagToken.GetAttributeValue(Constants.SectionNameAtt);
                        await childPage.RenderSection(openTagToken, writer);
                        continue;
                    }
                    else if (openTagToken.NodeName == Constants.DefineSectionTag)
                    {
                        enumerator.TakeUntil<CloseTagToken>(token => token?.NodeName == Constants.DefineSectionTag).ToList();
                        continue;
                    }
                    else if (openTagToken.NodeName == Constants.DocumentTag)
                    {
                        continue;
                    }
                    else if (renderContext.InterceptorFactory.IsDefined(renderContext, openTagToken.NodeName))
                    {
                        var innerTokens = Enumerable.Empty<IToken>();
                        if (!openTagToken.Closed)
                            innerTokens = enumerator.TakeUntil<CloseTagToken>(token => token?.NodeName == openTagToken.NodeName).ToList();

                        var interceptorResult = await renderContext.InterceptorFactory.RenderInterceptorAsync(renderContext, openTagToken, innerTokens, writer);
                        if (interceptorResult != null)
                        {
                            if (interceptorResult.Tokens?.Any() ?? false)
                                await RenderContentAsync(interceptorResult.Tokens, writer);

                            if (!String.IsNullOrEmpty(interceptorResult.Html))
                                writer.Write(interceptorResult.Html);
                        }
                        continue;
                    }
                    else
                    {
                        writer.Write(openTagToken.ToAttributedString(renderContext.Variables));
                        skipSpace = true;
                        continue;
                    }
                }

                if (enumerator.Current is CloseTagToken)
                {
                    var closeTagToken = (CloseTagToken)enumerator.Current;
                    if (closeTagToken.NodeName == Constants.SectionTag)
                    {
                        continue;
                    }
                    else
                    {
                        writer.Write(closeTagToken.ToString());
                        skipSpace = true;
                        continue;
                    }
                }

                if (enumerator.Current is VariableToken)
                {
                    var stringValue = String.Empty;
                    var variableToken = (VariableToken)enumerator.Current;
                    var variableName = variableToken.VariableName;
                    if (!String.IsNullOrEmpty(variableName) && renderContext.Variables.HasValue(variableName))
                    {
                        var value = this.renderContext.Variables.GetValue(variableName);
                        stringValue = value?.ToString() ?? String.Empty;
                    }
                    else
                    {
                        stringValue = variableToken.ToString();
                    }

                    writer.Write(stringValue);
                    skipSpace = false;
                    if (stringValue?.Length > 0)
                    {
                        lastWritten = stringValue[stringValue.Length - 1];
                    }

                    continue;
                }

                if (enumerator.Current is EntityToken)
                {
                    var entityToken = (EntityToken)enumerator.Current;
                    writer.Write(entityToken.ToString());
                }
            }
        }

        public override async Task RenderSection(OpenTagToken openTag, StreamWriter writer)
        {
            var sectionName = openTag.GetAttributeValue(Constants.SectionNameAtt);
            var sectionExists = sectionContent.ContainsKey(sectionName);

            var sectionExpected = false;
            if (openTag.HasAttribute(Constants.SectionRequiredAtt))
                sectionExpected = openTag.GetAttributeValue<bool?>(Constants.SectionRequiredAtt) ?? false;

            if (!sectionExists && sectionExpected)
                throw new ArgumentException($"A section with name {sectionName} has no content, but it was expected.");

            if (sectionExists)
                await RenderContentAsync(sectionContent[sectionName], writer);
        }
    }
}
