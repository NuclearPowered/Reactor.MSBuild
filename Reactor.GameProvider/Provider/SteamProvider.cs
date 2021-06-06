using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DepotDownloader;

namespace Reactor.GameProvider.Provider
{
    public class SteamProvider : BaseProvider
    {
        private const uint AppId = 945360;
        private const uint DepotId = 945361;

        public ulong Manifest { get; }

        public List<string> FilesToDownload { get; } = new List<string>
        {
            "GameAssembly.dll",
            "Among Us_Data/il2cpp_data/Metadata/global-metadata.dat",
            "Among Us_Data/globalgamemanagers",
            "Among Us_Data/Managed/Assembly-CSharp.dll"
        };

        public SteamProvider(string baseDirectory, ulong manifest)
        {
            Directory = Path.Combine(baseDirectory, "steam", manifest.ToString());
            Manifest = manifest;
        }

        public override string Directory { get; }

        public void Login()
        {
            if (ContentDownloader.steam3?.bConnected == true)
                return;

            AccountSettingsStore.LoadFromFile("account.config");

            var environmentVariable = Environment.GetEnvironmentVariable("STEAM");

            if (environmentVariable != null)
            {
                var split = environmentVariable.Split(':');
                if (!ContentDownloader.InitializeSteam3(split[0], split[1]))
                {
                    throw new ProviderConnectionException(this, "Incorrect credentials.");
                }
            }
            else
            {
                throw new ProviderConnectionException(this, "STEAM environment variable can't be empty.");
            }

            if (ContentDownloader.steam3 == null || !ContentDownloader.steam3.bConnected)
            {
                throw new ProviderConnectionException(this, "Unable to initialize Steam3 session.");
            }
        }

        public override async Task DownloadAsync()
        {
            DepotConfigStore.LoadFromFile(Path.Combine(Directory, ".DepotDownloader", "depot.config"));
            if (DepotConfigStore.Instance.InstalledManifestIDs.TryGetValue(DepotId, out var installedManifest))
            {
                if (installedManifest == Manifest)
                {
                    return;
                }
            }

            Login();

            ContentDownloader.Config.UsingFileList = true;
            ContentDownloader.Config.FilesToDownload = new HashSet<string>();
            ContentDownloader.Config.FilesToDownloadRegex = FilesToDownload.Select(CreatePathRegex).ToList();

            ContentDownloader.Config.InstallDirectory = Directory;
            await ContentDownloader.DownloadAppAsync(AppId, DepotId, Manifest);
        }

        public override void Dispose()
        {
            ContentDownloader.ShutdownSteam3();
        }

        private static Regex CreatePathRegex(string path)
        {
            return new Regex($"^{path}$".Replace("/", "[\\\\|/]"), RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}
