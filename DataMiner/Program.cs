using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ButlerdSharp;
using ButlerdSharp.Protocol.Requests;
using ButlerdSharp.Protocol.Structs;
using DepotDownloader;
using Mono.Cecil;
using Newtonsoft.Json;
using Reactor.GameProvider;
using Reactor.GameProvider.Provider;

namespace DataMiner
{
    internal class Program
    {
        public static string DataRepository { get; private set; }

        public static async Task Main(string[] args)
        {
            DataRepository = args[0];
            if (!Directory.Exists(DataRepository))
            {
                throw new Exception(DataRepository + " is not a directory");
            }

            Console.WriteLine("> Mining steam");
            await MineSteamAsync();

            Console.WriteLine("> Mining itch");
            await MineItchAsync();

            Console.WriteLine("> Finished");
        }

        private static async Task<(GameVersion gameVersion, RuntimeType runtimeType, bool isObfuscated)> MineGameAsync(string gameDirectory, GamePlatform gamePlatform)
        {
            var gameVersion = new GameVersion(GameVersionExtractor.Extract(Path.Combine(gameDirectory, "Among Us_Data", "globalgamemanagers")), gamePlatform);
            RuntimeType runtimeType;

            if (File.Exists(Path.Combine(gameDirectory, "GameAssembly.dll")))
            {
                runtimeType = RuntimeType.IL2CPP;
            }
            else if (File.Exists(Path.Combine(gameDirectory, "Among Us_Data", "Managed", "Assembly-CSharp.dll")))
            {
                runtimeType = RuntimeType.Mono;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(runtimeType));
            }

            var isObfuscated = false;

            if (runtimeType == RuntimeType.IL2CPP)
            {
                var unhollowerManager = new UnhollowerManager(gameDirectory);

                var dummyDllPath = Path.Combine(gameDirectory, "DummyDll", "Assembly-CSharp.dll");

                if (!File.Exists(dummyDllPath))
                {
                    unhollowerManager.Dump();
                }

                using (var moduleDefinition = ModuleDefinition.ReadModule(dummyDllPath))
                {
                    isObfuscated = moduleDefinition.Types.Any(x => x.Name.Length == 11 && x.Name.All(char.IsUpper));
                }

                var zipPath = Path.Combine(DataRepository, "unhollowed", gameVersion + ".zip");
                if (!File.Exists(zipPath))
                {
                    var bepInExPath = "BepInEx";
                    var bepInExManager = new BepInExManager(bepInExPath);
                    if (await bepInExManager.CheckIfUpdateRequiredAsync())
                    {
                        await bepInExManager.DownloadAsync();
                    }

                    unhollowerManager.BaseLibs = Path.Combine(bepInExPath, "BepInEx", "unity-libs");
                    unhollowerManager.MscorlibPath = Path.Combine(bepInExPath, "mono", "Managed", "mscorlib.dll");

                    unhollowerManager.Unhollow();

                    var files = Directory.GetFiles(unhollowerManager.UnhollowedPath, "*.dll");

                    await Task.WhenAll(files.Select(file => Task.Run(() => new Stubber(file).Stub())));

                    await using var fileStream = File.OpenWrite(zipPath);
                    using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);

                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                    }
                }
            }

            return (gameVersion, runtimeType, isObfuscated);
        }

        private static async Task MineSteamAsync()
        {
            Console.WriteLine("Currently there is no easy way to get list of all steam manifests so we have to rely on steamdb.info");
            Console.WriteLine("Please login into https://steamdb.info/depot/945361/manifests/, execute and paste results of");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"Array.from(document.querySelectorAll("".tabular-nums"")).map(x => x.innerText).join("";"")");

            Console.ForegroundColor = ConsoleColor.Gray;
            var manifests = Console.ReadLine()!.Trim('"').Split(";").Select(ulong.Parse).ToArray();
            Console.ResetColor();

            Console.WriteLine($"Loaded {manifests.Length} manifests");

            var map = new MultiMap<GameVersion, SteamBuildInfo>();

            var i = 0;
            foreach (var manifest in manifests)
            {
                i++;

                var steamProvider = new SteamProvider(Directory.GetCurrentDirectory(), manifest);
                await steamProvider.DownloadAsync();

                Console.WriteLine($"Mining {manifest} ({i}/{manifests.Length})");
                var (gameVersion, runtimeType, isObfuscated) = await MineGameAsync(steamProvider.Directory, GamePlatform.Steam);

                switch (manifest)
                {
                    // Version hardcoded inside VersionShower, would be very annoying to extract automatically
                    case 3941730972865408291:
                        gameVersion = new GameVersion("2021.3.31.3s");
                        break;
                    case 2517455626042488615:
                        gameVersion = new GameVersion("2021.3.31.2s");
                        break;
                    case 6481349447274145710:
                        gameVersion = new GameVersion("2019.8.8.1s");
                        break;

                    // Forte please get a build script, lmao
                    case 6261260287144428102:
                        gameVersion = new GameVersion("2021.3.5i");
                        continue;

                    // 2021.2.21, do you see anything wrong here? its a platformless build, fun
                    case 2052794034769266028:
                        gameVersion = new GameVersion("2021.2.21");
                        continue;

                    // Pushing new code update without bumping the version go brr
                    case 982761632128031721:
                    case 3948341867684505905:
                    case 7185019524463673033:
                    case 6320154229190358631:
                    case 8288448455957108838:
                    case 8584591370903023077:
                    case 9017577614281730844:
                        continue;
                }

                map.Add(gameVersion, new SteamBuildInfo(runtimeType, isObfuscated, manifest));
            }

            await File.WriteAllTextAsync(Path.Combine(DataRepository, "versions", "steam.json"), JsonConvert.SerializeObject(map, Formatting.Indented, new MultiMapJsonConverter<GameVersion, SteamBuildInfo>()));

            ContentDownloader.ShutdownSteam3();
        }

        private static async Task MineItchAsync()
        {
            using var client = new ButlerdClient();

            await client.UpgradeButlerAsync("./butler");
            await client.StartAsync("./butler.db");

            Console.WriteLine($"Started butler ({client.Version})");

            var environmentVariable = Environment.GetEnvironmentVariable("ITCH");
            var split = environmentVariable!.Split(":");

            var loginResponse = await new Requests.Profile.LoginWithPassword.Request(split[0], split[1], false).SendAsync(client.JsonRpc);
            var profile = loginResponse.Profile;

            Console.WriteLine($"Logged in as {profile.User.Username}");

            const int gameId = 257677; // Among Us

            await new Requests.Fetch.DownloadKeys.Request(profile.Id, fresh: true, filters: new FetchDownloadKeysFilter
            {
                GameId = gameId
            }).SendAsync(client.JsonRpc);

            var planResponse = await new Requests.Install.Plan.Request(gameId).SendAsync(client.JsonRpc);

            var upload = planResponse.Uploads.Single(x => x.Id == 1047908);
            var builds = (await new Requests.Fetch.UploadBuilds.Request(planResponse.Game, upload).SendAsync(client.JsonRpc)).Builds;

            Console.WriteLine($"Found {builds.Length} builds");

            var map = new MultiMap<GameVersion, ItchBuildInfo>();

            var i = 0;
            foreach (var build in builds)
            {
                i++;
                var installFolder = Path.Combine("itch", build.Id.ToString());
                var stagingFolder = Path.Combine("itch", "staging", build.Id.ToString());

                if (!Directory.Exists(installFolder) || Directory.Exists(stagingFolder))
                {
                    Console.WriteLine($"Downloading {build.Id} ({i}/{builds.Length})");

                    var queueResponse = await new Requests.Install.Queue.Request(
                        noCave: true,
                        installFolder: installFolder,
                        stagingFolder: stagingFolder,
                        ignoreInstallers: true,
                        game: planResponse.Game,
                        upload: upload,
                        build: build
                    ).SendAsync(client.JsonRpc);

                    await new Requests.Install.Perform.Request(Guid.NewGuid().ToString(), queueResponse.StagingFolder).SendAsync(client.JsonRpc);
                }

                Console.WriteLine($"Mining {build.Id} ({i}/{builds.Length})");
                var (gameVersion, runtimeType, isObfuscated) = await MineGameAsync(Path.Combine(installFolder, "AmongUs"), GamePlatform.Itch);

                switch (build.Id)
                {
                    // Version hardcoded inside VersionShower, would be very annoying to extract automatically
                    case 378293:
                        gameVersion = new GameVersion("2021.3.31.3s");
                        break;
                    case 378271:
                        gameVersion = new GameVersion("2021.3.31.2s");
                        break;
                    case 185934:
                        gameVersion = new GameVersion("2019.8.8.1s");
                        break;

                    // Pushing new code update without bumping the version go brr
                    case 367084:
                    case 325830:
                    case 267643:
                    case 258113:
                    case 206710:
                    case 141422:
                        continue;
                }

                map.Add(gameVersion, new ItchBuildInfo(runtimeType, isObfuscated, build.Id));
            }

            await File.WriteAllTextAsync(Path.Combine(DataRepository, "versions", "itch.json"), JsonConvert.SerializeObject(map, Formatting.Indented, new MultiMapJsonConverter<GameVersion, ItchBuildInfo>()));
        }
    }
}
