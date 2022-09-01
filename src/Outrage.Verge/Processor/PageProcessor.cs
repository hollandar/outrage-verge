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

namespace Outrage.Verge.Processor
{
    public class PageProcessor
    {
        private readonly ContentLibrary contentLibrary;
        private readonly InterceptorFactory interceptorFactory;
        private readonly Variables variables;
        private readonly IDictionary<string, List<IToken>> sectionContent = new Dictionary<string, List<IToken>>();
        private IEnumerable<IToken> tokens;
        private PageProcessor? layoutPage;
        private PageProcessor? childPage;
        private bool skipSpace = true;
        private char lastWritten = char.MinValue;

        protected PageProcessor(string contentName, PageProcessor childPage, ContentLibrary contentLibrary, InterceptorFactory interceptorFactory, Variables variables)
        {
            this.contentLibrary = contentLibrary;
            this.interceptorFactory = interceptorFactory;
            this.variables = variables;
            this.childPage = childPage;
            this.Process(contentName);
        }

        public PageProcessor(string contentName, ContentLibrary contentLibrary, InterceptorFactory interceptorFactory, Variables variables)
        {
            this.contentLibrary = contentLibrary;
            this.interceptorFactory = interceptorFactory;
            this.variables = variables;
            this.Process(contentName);
        }

        protected void Process(string contentName)
        {
            if (!contentLibrary.ContentExists(contentName))
                throw new ArgumentException($"{contentName} is unknown.");

            tokens = this.contentLibrary.GetHtml(contentName);

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
            if (!contentLibrary.ContentExists(templateVariable))
                throw new ArgumentException($"A layout with the name {templateVariable} does not exist.");

            if (this.layoutPage != null)
                throw new ArgumentException("Template page can not be set twice, remove the second template tag.");

            this.layoutPage = new PageProcessor(templateVariable, this, contentLibrary, this.interceptorFactory, this.variables);
        }

        public string Render()
        {
            var builder = new StringBuilder();
            this.Render(builder);

            return builder.ToString();
        }

        protected void Render(StringBuilder builder)
        {
            if (this.layoutPage != null)
                this.layoutPage.Render(builder);
            else
                RenderContent(this.tokens, builder);

        }

        protected void RenderContent(IEnumerable<IToken> tokens, StringBuilder builder)
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
                            builder.Append(c);
                            lastWritten = c;
                            continue;
                        }


                        if (c != ' ' && c != '\r' && c != '\n')
                        {
                            builder.Append(c);
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

                    if (openTagToken.NodeName == Constants.SectionTag && this.childPage != null)
                    {
                        var sectionName = openTagToken.GetAttributeValue(Constants.SectionNameAtt);
                        childPage.RenderSection(openTagToken, builder);
                        continue;
                    }
                    else if (openTagToken.NodeName == Constants.IncludeTag)
                    {
                        var contentName = openTagToken.GetAttributeValue(Constants.IncludeNameAtt);
                        RenderInclude(contentName, builder);
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
                    else if (interceptorFactory.IsDefined(openTagToken.NodeName))
                    {
                        var innerTokens = Enumerable.Empty<IToken>();
                        if (!openTagToken.Closed)
                            tokens = enumerator.TakeUntil<CloseTagToken>(token => token.NodeName == openTagToken.NodeName);

                        var interceptorTokens = this.interceptorFactory.RenderInterceptor(openTagToken, tokens, builder);
                        if (interceptorTokens?.Any() ?? false)
                            this.RenderContent(interceptorTokens, builder);
                    }
                    else
                    {
                        builder.Append(openTagToken.ToString());
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
                    else if (closeTagToken.NodeName == Constants.IncludeTag)
                    {
                        continue;
                    }
                    else
                    {
                        builder.Append(closeTagToken.ToString());
                        skipSpace = true;
                        continue;
                    }
                }
            }
        }

        protected void RenderSection(OpenTagToken openTag, StringBuilder builder)
        {
            var sectionName = openTag.GetAttributeValue(Constants.SectionNameAtt);
            var sectionExists = this.sectionContent.ContainsKey(sectionName);

            var sectionExpected = false;
            if (openTag.HasAttribute(Constants.SectionRequiredAtt))
                sectionExpected = openTag.GetAttributeValue<bool?>(Constants.SectionRequiredAtt) ?? false;

            if (!sectionExists && sectionExpected)
                throw new ArgumentException($"A section with name {sectionName} has no content, but it was expected.");

            if (sectionExists)
                this.RenderContent(this.sectionContent[sectionName], builder);
        }

        protected void RenderInclude(string contentName, StringBuilder builder)
        {
            if (!this.contentLibrary.ContentExists(contentName))
                throw new ArgumentException($"No content with the name {contentName} exists.");

            var pageProcessor = new PageProcessor(contentName, this.contentLibrary, this.interceptorFactory, this.variables);
            pageProcessor.Render(builder);
        }

        protected string HandleVariables(string input)
        {
            return this.variables.ReplaceVariables(input);
        }
    }
}
