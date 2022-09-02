using Microsoft.Extensions.DependencyInjection;

namespace Outrage.Verge.Processor
{
    public class ProcessorFactory
    {
        private readonly Dictionary<string, IProcessorFactory> factories = new();
        public ProcessorFactory(IServiceProvider serviceProvider)
        {
            var factories = serviceProvider.GetService<IEnumerable<IProcessorFactory>>();
            if (factories?.Any() ?? false)
            {
                foreach (var factory in factories)
                {
                    this.factories[factory.GetExtension()] = factory;
                }
            }
            else
                throw new ArgumentException("No processor factories are registered.");
        }

        public IProcessorFactory Get(string extension)
        {
            if (factories.TryGetValue(extension, out var processorFactory))
            {
                return processorFactory;
            }

            throw new ArgumentException($"No factory is registered for extension {extension}.");
        }
    }
}
