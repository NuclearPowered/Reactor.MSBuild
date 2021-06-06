using System.IO;
using System.Linq;
using System.Text;

namespace Reactor.GameProvider
{
    public static class GameVersionExtractor
    {
        public static int IndexOf(this byte[] source, byte[] pattern)
        {
            for (var i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <param name="file">Path to Among Us_Data/globalgamemanagers</param>
        public static string Extract(string file)
        {
            var bytes = File.ReadAllBytes(file);

            var pattern = Encoding.UTF8.GetBytes("public.app-category.games");
            var index = bytes.IndexOf(pattern) + pattern.Length;

            pattern = Encoding.UTF8.GetBytes("20"); // this should work for some time lol
            index += bytes.Skip(index).ToArray().IndexOf(pattern);

            return Encoding.UTF8.GetString(bytes.Skip(index).TakeWhile(x => x != 0).ToArray());
        }
    }
}
