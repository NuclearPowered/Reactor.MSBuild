using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Reactor.MSBuild
{
    public static class Context
    {
        public static string CachePath { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? Path.Combine(Environment.GetEnvironmentVariable("XDG_CACHE_HOME") ?? Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? ".", ".cache"), "reactor")
            // Path#Combine is borken on visual studio
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + ".reactor";
    }
}
