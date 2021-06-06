using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Reactor.GameProvider.Provider
{
    public class GithubProvider : BaseProvider
    {
        public override bool RequiresUnhollowing => false;

        public override string Directory { get; }

        public string DataRepository { get; }
        public GameVersion GameVersion { get; }

        public GithubProvider(string baseDirectory, string dataRepository, GameVersion gameVersion)
        {
            Directory = Path.Combine(baseDirectory, "github", gameVersion.ToString());
            DataRepository = dataRepository;
            GameVersion = gameVersion;
        }

        public override async Task DownloadAsync()
        {
            var unhollowed = Path.Combine(Directory, "BepInEx", "unhollowed");

            // TODO hash?
            if (!System.IO.Directory.Exists(unhollowed))
            {
                using var httpClient = new HttpClient();
                using var stream = await httpClient.GetStreamAsync($"https://raw.githubusercontent.com/{DataRepository}/master/unhollowed/{GameVersion}.zip");
                using var zipArchive = new ZipArchive(stream);

                zipArchive.ExtractToDirectory(unhollowed);
            }
        }
    }
}
