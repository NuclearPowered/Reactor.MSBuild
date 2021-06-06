using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Reactor.GameProvider
{
    public class BepInExManager
    {
        public async Task DownloadAsync(string directory)
        {
            // TODO improve this (by upstreaming NuclearPowered/BepInEx)
            if (File.Exists(Path.Combine(directory, "BepInEx", "core", "BepInEx.IL2CPP.dll")))
            {
                return;
            }

            using var httpClient = new HttpClient();
            using var zipStream = await httpClient.GetStreamAsync("https://github.com/NuclearPowered/BepInEx/releases/download/6.0.0-reactor.18%2Bstructfix/BepInEx-6.0.0-reactor.18+structfix.zip");
            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            zipArchive.ExtractToDirectory(directory);
        }
    }
}
