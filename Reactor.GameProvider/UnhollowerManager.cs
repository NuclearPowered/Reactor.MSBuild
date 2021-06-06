using System.IO;
using System.Security.Cryptography;
using System.Text;
using AssemblyUnhollower;

namespace Reactor.GameProvider
{
    public class UnhollowerManager
    {
        public string GameDirectory { get; }

        public string GameAssemblyPath => Path.Combine(GameDirectory, "GameAssembly.dll");

        public string BaseLibs { get; set; }
        public string MscorlibPath { get; set; }

        public string UnhollowedPath => Path.Combine(GameDirectory, "BepInEx", "unhollowed");
        public string HashPath => Path.Combine(UnhollowedPath, "assembly-hash.txt");

        public UnhollowerManager(string gameDirectory)
        {
            GameDirectory = gameDirectory;

            BaseLibs = Path.Combine(GameDirectory, "BepInEx", "unity-libs");
            MscorlibPath = Path.Combine(GameDirectory, "mono", "Managed", "mscorlib.dll");
        }

        private static string ByteArrayToString(byte[] data)
        {
            var builder = new StringBuilder(data.Length * 2);

            foreach (var b in data)
                builder.AppendFormat("{0:x2}", b);

            return builder.ToString();
        }

        private string ComputeHash()
        {
            using var md5 = MD5.Create();

            var gameAssemblyBytes = File.ReadAllBytes(GameAssemblyPath);
            md5.TransformBlock(gameAssemblyBytes, 0, gameAssemblyBytes.Length, gameAssemblyBytes, 0);

            if (Directory.Exists(BaseLibs))
            {
                foreach (var file in Directory.EnumerateFiles(BaseLibs, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    var pathBytes = Encoding.UTF8.GetBytes(Path.GetFileName(file));
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    var contentBytes = File.ReadAllBytes(file);
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }
            }

            md5.TransformFinalBlock(new byte[0], 0, 0);

            return ByteArrayToString(md5.Hash);
        }

        public bool CheckIfGenerationRequired()
        {
            if (!Directory.Exists(UnhollowedPath))
                return true;

            if (!File.Exists(HashPath))
                return true;

            if (ComputeHash() != File.ReadAllText(HashPath))
            {
                return true;
            }

            return false;
        }

        public void Dump()
        {
            var dumperConfig = new Il2CppDumper.Config
            {
                GenerateStruct = false,
                GenerateDummyDll = true
            };

            Il2CppDumper.Il2CppDumper.PerformDump(
                GameAssemblyPath,
                Path.Combine(GameDirectory, "Among Us_Data", "il2cpp_data", "Metadata", "global-metadata.dat"),
                GameDirectory, dumperConfig, _ =>
                {
                }
            );
        }

        public void Unhollow()
        {
            var unhollowerOptions = new UnhollowerOptions
            {
                GameAssemblyPath = GameAssemblyPath,
                MscorlibPath = MscorlibPath,
                SourceDir = Path.Combine(GameDirectory, "DummyDll"),
                OutputDir = UnhollowedPath,
                UnityBaseLibsDir = BaseLibs,
                NoCopyUnhollowerLibs = true
            };

            Program.Main(unhollowerOptions);
        }

        public void Run()
        {
            Dump();
            Unhollow();

            File.WriteAllText(HashPath, ComputeHash());
        }
    }
}
