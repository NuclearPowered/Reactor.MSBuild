using System;
using System.Threading.Tasks;

namespace Reactor.GameProvider.Provider
{
    public abstract class BaseProvider : IDisposable
    {
        public virtual bool RequiresUnhollowing => true;

        public abstract string Directory { get; }

        public abstract Task DownloadAsync();

        public virtual void Dispose()
        {
        }
    }

    public class ProviderConnectionException : Exception
    {
        public BaseProvider Provider { get; }

        public ProviderConnectionException(BaseProvider provider, string message) : base(message)
        {
            Provider = provider;
        }
    }
}
