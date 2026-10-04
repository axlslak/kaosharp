using AOSharp.Common.GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Navigator
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            return new Vector3((float)token[0], (float)token[1], (float)token[2]);
        }

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class Vector3ListConverter : JsonConverter<List<Vector3>>
    {
        public override List<Vector3> ReadJson(JsonReader reader, Type objectType, List<Vector3> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            return token.Select(x => new Vector3((float)x[0], (float)x[1], (float)x[2])).ToList();
        }

        public override void WriteJson(JsonWriter writer, List<Vector3> value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
