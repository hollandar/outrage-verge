using Outrage.Verge.Library;
using Outrage.Verge.Parser;
using Outrage.Verge.Parser.Tokens;
using Outrage.Verge.Processor;
using Outrage.TokenParser.Tokens;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Compose.Serialize;
using Outrage.Verge.Configuration;

var contentLibraryObject = new ContentLibrary("g:\\scratch\\htmlt");

var siteProcessor = new SiteProcessor("g:\\scratch\\htmlt", "c:\\temp\\outputSite");
var stopwatch = new Stopwatch();
stopwatch.Start();
siteProcessor.Process();
stopwatch.Stop();

Console.WriteLine($"Processing took: {stopwatch.ElapsedMilliseconds}.");




