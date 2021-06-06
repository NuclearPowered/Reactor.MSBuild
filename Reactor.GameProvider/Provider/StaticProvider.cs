using System.Threading.Tasks;

namespace Reactor.GameProvider.Provider
{
    public class StaticProvider : BaseProvider
    {
        public StaticProvider(string directory)
        {
            Directory = directory;
        }

        public override string Directory { get; }

        public override Task DownloadAsync()
        {
            return Task.CompletedTask;
        }
    }
}
