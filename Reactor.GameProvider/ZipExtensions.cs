// Polyfilled version of .NET 5 https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.zipfileextensions.extracttodirectory

#if NETFRAMEWORK
using System;
using System.IO;
#endif
using System.IO.Compression;

namespace Reactor.GameProvider
{
    public static class ZipExtensions
    {
        public static void ExtractToDirectoryOverwrite(this ZipArchive archive, string destinationDirectoryName)
        {
#if NETFRAMEWORK
            if (archive == null)
                throw new ArgumentNullException(nameof(archive));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            foreach (ZipArchiveEntry source in archive.Entries)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));

                if (destinationDirectoryName == null)
                    throw new ArgumentNullException(nameof(destinationDirectoryName));

                // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
                DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
                string destinationDirectoryFullPath = di.FullName;
                if (!destinationDirectoryFullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    destinationDirectoryFullPath += Path.DirectorySeparatorChar;

                string fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, source.FullName));

                if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Extracting Zip entry would have resulted in a file outside the specified destination directory.");

                if (Path.GetFileName(fileDestinationPath).Length == 0)
                {
                    // If it is a directory:

                    if (source.Length != 0)
                        throw new IOException("Zip entry name ends in directory separator character but contains data.");

                    Directory.CreateDirectory(fileDestinationPath);
                }
                else
                {
                    // If it is a file:
                    // Create containing directory:
                    Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
                    source.ExtractToFile(fileDestinationPath, true);
                }
            }
#else
            archive.ExtractToDirectory(destinationDirectoryName, true);
#endif
        }
    }
}
