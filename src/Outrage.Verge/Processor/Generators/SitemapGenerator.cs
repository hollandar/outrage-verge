using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Outrage.Verge.Processor.Generators
{
    public class SitemapGenerator : IContentGenerator
    {
        public List<string> locations = new();
        public Task ContentUpdated(RenderContext renderContext, string contentUri, ContentName contentName)
        {
            locations.Add(contentUri);
            return Task.CompletedTask;
        }

        public Task Finalize(RenderContext renderContext)
        {
            var uriName = renderContext.SiteConfiguration.UriName;
            var xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var xmlDocument = new XDocument(new XElement(XName.Get("sitemapindex", xmlns)));

            foreach (var location in locations)
            {
                var locElement = new XElement(XName.Get("loc", xmlns), new XText($"{uriName}{location}"));
                var lastmodElement = new XElement(XName.Get("lastmod", xmlns), new XText(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00")));
                var sitemapElement = new XElement(XName.Get("sitemap", xmlns), locElement, lastmodElement);
                xmlDocument.Root!.Add(sitemapElement);
            }

            using var writer = renderContext.PublishLibrary.OpenWriter(ContentName.From("sitemap.xml"));
            using var xmlWriter = XmlWriter.Create(writer);
            xmlDocument.WriteTo(xmlWriter);

            return Task.CompletedTask;
        }
    }
}
