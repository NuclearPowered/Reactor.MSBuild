using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Reactor.GameProvider
{
    [JsonConverter(typeof(GameVersionJsonConverter))]
    public class GameVersion
    {
        private static readonly Regex _regex = new Regex(@"^(?<year>[0-9]+)\.(?<month>[0-9]+)\.(?<day>[0-9]+)(\.(?<patch>[0-9]+))?(?<platform>[siaoem])?", RegexOptions.Compiled);

        public static GamePlatform GamePlatformFromShorthand(string shorthand)
        {
            return shorthand switch
            {
                "s" => GamePlatform.Steam,
                "i" => GamePlatform.Itch,
                "a" => GamePlatform.Android,
                "o" => GamePlatform.IOS,
                "e" => GamePlatform.Epic,
                "m" => GamePlatform.MicrosoftStore,
                _ => throw new ArgumentOutOfRangeException(nameof(shorthand))
            };
        }

        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        public int Patch { get; }
        public GamePlatform? Platform { get; }

        public GameVersion(string version, GamePlatform? fallbackPlatform = null)
        {
            var match = _regex.Match(version);

            Year = int.Parse(match.Groups["year"].Value);
            Month = int.Parse(match.Groups["month"].Value);
            Day = int.Parse(match.Groups["day"].Value);
            Patch = match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0;

            var platform = match.Groups["platform"];
            Platform = platform.Success && !string.IsNullOrEmpty(platform.Value) ? GamePlatformFromShorthand(platform.Value) : fallbackPlatform;
        }

        public override string ToString()
        {
            return $"{Year}.{Month}.{Day}{(Patch == 0 ? string.Empty : $".{Patch}")}" + Platform switch
            {
                GamePlatform.Steam => "s",
                GamePlatform.Itch => "i",
                GamePlatform.Android => "a",
                GamePlatform.IOS => "o",
                GamePlatform.Epic => "e",
                GamePlatform.MicrosoftStore => "m",
                null => string.Empty,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        protected bool Equals(GameVersion other)
        {
            return Year == other.Year && Month == other.Month && Day == other.Day && Patch == other.Patch && Platform == other.Platform;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GameVersion) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Year, Month, Day, Patch, Platform);
        }

        public static bool TryParse(string raw, [NotNullWhen(true)] out GameVersion? gameVersion)
        {
            try
            {
                gameVersion = new GameVersion(raw);
                return true;
            }
            catch
            {
                gameVersion = null;
                return false;
            }
        }

        public static explicit operator GameVersion(string value) => new GameVersion(value);
    }

    public enum GamePlatform
    {
        Steam,
        Itch,
        Android,
        IOS,
        Epic,
        MicrosoftStore
    }
}
