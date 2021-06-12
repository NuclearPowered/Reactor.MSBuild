using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Reactor.GameProvider;
using Reactor.GameProvider.Provider;
using UnhollowerBaseLib;

namespace Reactor.MSBuild
{
    public class DownloadGame : AsyncTask
    {
        [Required]
        public string GameProvider { get; set; }

        [Required]
        public string GameVersion { get; set; }

        public string DataRepository { get; set; } = "NuclearPowered/Data";

        [Output]
        public string AmongUsPath { get; set; }

        public Regex GithubRegex { get; } = new Regex(@"^(?<org>[A-Za-z0-9_.-]+)\/(?<repo>[A-Za-z0-9_.-]+)$");

        public override async Task<bool> ExecuteAsync()
        {
            if (!GithubRegex.IsMatch(DataRepository))
            {
                Log.LogError("Data repository is invalid");
                return false;
            }

            Directory.CreateDirectory(Context.CachePath);
            var lockFilePath = Path.Combine(Context.CachePath, ".lock");

            FileStream lockFile = null;

            do
            {
                try
                {
                    lockFile = File.Open(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException e)
                {
                    // TODO Replace with https://github.com/dotnet/runtime/issues/926
                    const int EAGAIN = 11;

                    const int ERROR_SHARING_VIOLATION = 32;
                    const int ERROR_LOCK_VIOLATION = 33;

                    var errorCode = e.HResult & ((1 << 16) - 1);
                    var isLocked = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? errorCode == EAGAIN
                        : errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
                    if (!isLocked)
                    {
                        throw;
                    }

                    Log.LogWarning("Cache is locked, retrying in a second");
                    await Task.Delay(1000);
                }
            } while (lockFile == null);

            try
            {
                using var httpClient = new HttpClient();

                BaseProvider provider;

                switch (GameProvider)
                {
                    case "Steam":
                    {
                        var gameVersion = new GameVersion(GameVersion);
                        var steamBuildInfos = JsonConvert.DeserializeObject<Dictionary<GameVersion, SteamBuildInfo>>(await httpClient.GetStringAsync($"https://raw.githubusercontent.com/{DataRepository}/master/versions/steam.json"));
                        if (steamBuildInfos == null)
                        {
                            Log.LogError("Failed to fetch steam versions");
                            return false;
                        }

                        if (steamBuildInfos.TryGetValue(gameVersion, out var steamBuildInfo))
                        {
                            if (steamBuildInfo.RuntimeType != RuntimeType.IL2CPP)
                            {
                                Log.LogError("Non IL2CPP builds are not supported");
                                return false;
                            }

                            if (steamBuildInfo.IsObfuscated)
                            {
                                Log.LogError("Obfuscated builds are not supported at the moment");
                                return false;
                            }

                            provider = new SteamProvider(Context.CachePath, steamBuildInfo.ManifestId);
                        }
                        else
                        {
                            Log.LogError($"{GameVersion} was not found in the version map");
                            return false;
                        }

                        break;
                    }

                    case "Itch":
                    {
                        Log.LogError("Itch support is WIP");
                        return false;
                    }

                    case "Github":
                    {
                        var gameVersion = new GameVersion(GameVersion);
                        provider = new GithubProvider(Context.CachePath, DataRepository, gameVersion);
                        break;
                    }

                    case "Static":
                    {
                        if (Directory.Exists(GameVersion))
                        {
                            provider = new StaticProvider(GameVersion);
                        }
                        else
                        {
                            Log.LogError("GameVersion has to be a path");
                            return false;
                        }

                        break;
                    }

                    default:
                    {
                        Log.LogError("Unsupported game provider: {0}", GameProvider);
                        return false;
                    }
                }

                Log.LogMessage("Downloading the game using " + provider.GetType().Name);
                await provider.DownloadAsync();

                AmongUsPath = provider.Directory;

                var bepInExManager = new BepInExManager(AmongUsPath);

                if (provider.RequiresBepInEx && await bepInExManager.CheckIfUpdateRequiredAsync())
                {
                    Log.LogMessage("Downloading BepInEx");
                    await bepInExManager.DownloadAsync();
                }

                var unhollowerManager = new UnhollowerManager(AmongUsPath);

                if (provider.RequiresUnhollowing && unhollowerManager.CheckIfGenerationRequired())
                {
                    Log.LogMessage("Unhollowing");

                    LogSupport.WarningHandler += s => Log.LogWarning(s);
                    LogSupport.ErrorHandler += s => Log.LogError(s);

                    unhollowerManager.Run();
                }

                provider.Dispose();

                return true;
            }
            finally
            {
                lockFile.Dispose();
                File.Delete(lockFilePath);
            }
        }
    }
}
