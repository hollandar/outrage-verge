using Compose.Path;
using Outrage.Verge.Configuration;
using Outrage.Verge.Library;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Processor
{
    public class LibraryContext
    {
        public LibraryContext(ContentName libraryBase, LibConfiguration libConfiguration)
        {
            LibraryBase = libraryBase;
            LibConfiguration = libConfiguration;
        }

        public ContentName LibraryBase { get; }
        public LibConfiguration LibConfiguration { get; }
    }

    public class LibraryFactory
    {
        private readonly ContentLibrary contentLibrary;
        private readonly ContentName libraryBase;

        private readonly Dictionary<string, LibConfiguration?> loadedLibs = new();

        public LibraryFactory(ContentLibrary contentLibrary, ContentName libraryBase)
        {
            this.contentLibrary = contentLibrary;
            this.libraryBase = libraryBase;
        }

        public LibraryContext? Get()
        {
            var libraryConfigurationName = libraryBase / "lib";
            LibConfiguration? libConfiguration;
            if (!loadedLibs.TryGetValue(libraryBase, out libConfiguration))
            {
                libConfiguration = this.contentLibrary.Deserialize<LibConfiguration>(libraryConfigurationName);
                if (libConfiguration == null)
                    throw new Exception($"Could not load lib.json/lib.yaml for library {libraryBase}");
                loadedLibs[libraryBase] = libConfiguration;
            }

            return new LibraryContext(this.libraryBase, libConfiguration!);
        }
    }
}
