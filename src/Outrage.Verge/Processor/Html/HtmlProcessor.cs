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

namespace Outrage.Verge.Processor.Html
{
    public class HtmlProcessor : ProcessorBase, IProcessor
    {
        private readonly IDictionary<string, List<IToken>> sectionContent = new Dictionary<string, List<IToken>>();
        private IEnumerable<IToken> tokens;
        private bool skipSpace = true;
        private char lastWritten = char.MinValue;

        public HtmlProcessor(string contentName, IProcessor childPage, RenderContext renderContext) : base(renderContext, childPage)
        {
            Load(contentName);
        }

        public HtmlProcessor(string contentName, RenderContext renderContext) : base(renderContext)
        {
            Load(contentName);
        }

        public HtmlProcessor(IEnumerable<IToken> tokens, RenderContext renderContext) : base(renderContext)
        {
            this.tokens = tokens;
            Process();
        }

        public void Load(string contentName)
        {
            if (!renderContext.ContentLibrary.ContentExists(contentName))
                throw new ArgumentException($"{contentName} is unknown.");

            tokens = renderContext.ContentLibrary.GetHtml(contentName);

            Process();
        }

        protected void Process()
        {
            var enumerator = new SpecialEnumerator<IToken>(tokens);
            while (enumerator.MoveNext())
            {
                var token = enumerator.Current;
                if (token is OpenTagToken)
                {
                    var openToken = (OpenTagToken)token;
                    if (openToken.NodeName == Constants.DefineSectionTag)
                    {
                        DefineSection(openToken, enumerator.TakeUntil<CloseTagToken>(endSection => endSection.NodeName == Constants.DefineSectionTag));
                    }
                    if (openToken.NodeName == Constants.TemplateTag)
                    {
                        if (!openToken.Closed)
                            throw new ArgumentException($"Template tag should be self closing.");
                        SetTemplate(openToken);
                    }
                }
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

        protected void SetTemplate(OpenTagToken openToken)
        {
            var templateName = openToken.GetAttributeValue(Constants.TemplateLayoutAtt);
            var templateVariable = HandleVariables(templateName);
            if (!renderContext.ContentLibrary.ContentExists(templateVariable))
                throw new ArgumentException($"A layout with the name {templateVariable} does not exist.");

            if (layoutPage != null)
                throw new ArgumentException("Template page can not be set twice, remove the second template tag.");

            layoutPage = new HtmlProcessor(templateVariable, this, this.renderContext);
        }

        public override void RenderToStream(Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true);
            RenderToStream(writer);
        }

        public override void RenderToStream(StreamWriter stream)
        {
            if (layoutPage != null)
                layoutPage.RenderToStream(stream);
            else
                RenderContent(tokens, stream);

        }

        protected void RenderContent(IEnumerable<IToken> tokens, StreamWriter writer)
        {
            var enumerator = new SpecialEnumerator<IToken>(tokens);
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
                        childPage.RenderSection(openTagToken, writer);
                        continue;
                    }
                    else if (openTagToken.NodeName == Constants.DefineSectionTag)
                    {
                        enumerator.TakeUntil<CloseTagToken>(token => token.NodeName == Constants.DefineSectionTag).ToList();
                        continue;
                    }
                    else if (openTagToken.NodeName == Constants.TemplateTag)
                    {
                        continue;
                    }
                    else if (renderContext.InterceptorFactory.IsDefined(openTagToken.NodeName))
                    {
                        var innerTokens = Enumerable.Empty<IToken>();
                        if (!openTagToken.Closed)
                            tokens = enumerator.TakeUntil<CloseTagToken>(token => token.NodeName == openTagToken.NodeName).ToList();

                        var interceptorTokens = renderContext.InterceptorFactory.RenderInterceptor(renderContext, openTagToken, tokens, writer);
                        if (interceptorTokens?.Any() ?? false)
                            RenderContent(interceptorTokens, writer);
                    }
                    else
                    {
                        writer.Write(openTagToken.ToString());
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
                    var variableToken = (VariableToken)enumerator.Current;
                    var variableName = variableToken.VariableName;
                    if (!String.IsNullOrEmpty(variableName) && renderContext.Variables.HasValue(variableName))
                    {
                        var value = this.renderContext.Variables.GetValue(variableName);
                        writer.Write(value.ToString());
                    } else
                    {
                        writer.Write(variableToken.ToString());
                    }
                    continue;
                }
            }
        }

        public override void RenderSection(OpenTagToken openTag, StreamWriter writer)
        {
            var sectionName = openTag.GetAttributeValue(Constants.SectionNameAtt);
            var sectionExists = sectionContent.ContainsKey(sectionName);

            var sectionExpected = false;
            if (openTag.HasAttribute(Constants.SectionRequiredAtt))
                sectionExpected = openTag.GetAttributeValue<bool?>(Constants.SectionRequiredAtt) ?? false;

            if (!sectionExists && sectionExpected)
                throw new ArgumentException($"A section with name {sectionName} has no content, but it was expected.");

            if (sectionExists)
                RenderContent(sectionContent[sectionName], writer);
        }

        protected string HandleVariables(string input)
        {
            return renderContext.Variables.ReplaceVariables(input);
        }
    }
}
