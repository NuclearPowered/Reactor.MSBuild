using System;
using Newtonsoft.Json;

namespace Reactor.GameProvider
{
    public class GameVersionJsonConverter : JsonConverter<GameVersion>
    {
        public override void WriteJson(JsonWriter writer, GameVersion? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.ToString());
            }
        }

        public override GameVersion? ReadJson(JsonReader reader, Type objectType, GameVersion? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var raw = reader.ReadAsString();
            return raw == null ? null : new GameVersion(raw);
        }
    }
}
