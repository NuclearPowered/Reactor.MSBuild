using System;
using Newtonsoft.Json;

namespace DataMiner
{
    public class MultiMapJsonConverter<TKey, TValue> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MultiMap<TKey, TValue>);
        }

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            var map = (MultiMap<TKey, TValue>) value;
            foreach (var k in map.Keys)
            {
                foreach (var v in map[k])
                {
                    writer.WritePropertyName(k.ToString()!);
                    serializer.Serialize(writer, v);
                }
            }

            writer.WriteEndObject();
        }
    }
}
