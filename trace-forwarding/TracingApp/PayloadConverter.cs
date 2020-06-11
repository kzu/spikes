using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TracingApp
{
    partial class Program
    {
        public class PayloadConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType== JsonToken.StartArray)
                    // This is the shape of the DiagnosticSource payload for the data
                    return serializer.Deserialize<List<Dictionary<string, object>>>(reader);

                return serializer.Deserialize(reader, objectType);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
