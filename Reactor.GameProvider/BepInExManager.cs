using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Cecil;
using Newtonsoft.Json;
using Version = SemVer.Version;

namespace Reactor.GameProvider
{
    public class BepInExManager
    {
        public class Release
        {
            [JsonConstructor]
            public Release(string tagName, IReadOnlyList<Asset> assets)
            {
                TagName = tagName;
                Assets = assets;
            }

            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            [JsonProperty("assets")]
            public IReadOnlyList<Asset> Assets { get; set; }

            public class Asset
            {
                [JsonConstructor]
                public Asset(string browserDownloadUrl)
                {
                    BrowserDownloadUrl = browserDownloadUrl;
                }

                [JsonProperty("browser_download_url")]
                public string BrowserDownloadUrl { get; set; }
            }
        }

        public string Directory { get; }

        public Release? LatestRelease { get; private set; }

        public BepInExManager(string directory)
        {
            Directory = directory;
        }

        public Version? GetCurrentVersion()
        {
            var path = Path.Combine(Directory, "BepInEx", "core", "BepInEx.IL2CPP.dll");

            if (!File.Exists(path))
            {
                return null;
            }

            using var assembly = AssemblyDefinition.ReadAssembly(path);
            var attribute = assembly.CustomAttributes.SingleOrDefault(x => x.AttributeType.FullName == typeof(AssemblyInformationalVersionAttribute).FullName);

            if (attribute == null)
            {
                return null;
            }

            return Version.Parse((string) attribute.ConstructorArguments.Single().Value);
        }

        public async Task<bool> CheckIfUpdateRequiredAsync()
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Reactor.GameProvider");

            string json;

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/NuclearPowered/BepInEx/releases/latest"))
            {
                var etagPath = Path.Combine(Directory, "BepInEx.etag");
                if (File.Exists(etagPath))
                {
                    requestMessage.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(File.ReadAllText(etagPath)));
                }

                var responseMessage = await httpClient.SendAsync(requestMessage);

                if (responseMessage.StatusCode == HttpStatusCode.NotModified)
                {
                    return false;
                }

                json = await responseMessage.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

                File.WriteAllText(Path.Combine(Directory, "BepInEx.etag"), responseMessage.Headers.ETag.ToString());
            }

            LatestRelease = JsonConvert.DeserializeObject<Release>(json)!;

            var currentVersion = GetCurrentVersion();

            return currentVersion == null || !currentVersion.PreRelease.StartsWith("reactor") || Version.Parse(LatestRelease.TagName) > currentVersion;
        }

        public async Task DownloadAsync()
        {
            if (LatestRelease == null)
            {
                throw new NullReferenceException("LatestRelease is null, call CheckIfUpdateRequiredAsync first");
            }

            using var httpClient = new HttpClient();
            using var zipStream = await httpClient.GetStreamAsync(LatestRelease.Assets.Single().BrowserDownloadUrl);
            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            zipArchive.ExtractToDirectoryOverwrite(Directory);
        }
    }
}
