using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SLS.SaveData
{
    public abstract class Saveable
    {
        public abstract string Name { get; }

        public abstract void EstablishActive();

        public static void InitializeSystem(Saveable asset, ref string defaultSnapot, ref Saveable activeRoot)
        {
            WriteSnap(asset, out defaultSnapot);
            activeRoot = JsonConvert.DeserializeObject<Saveable>(defaultSnapot, DefaultSerializer);
            activeRoot.EstablishActive();
        }

        public virtual JsonSerializerSettings Serializer => DefaultSerializer;
        public virtual JToken WriteToken(JsonSerializer serializer)
        {
            // Use JObject.FromObject to produce a token; serializer should be constructed without this converter to avoid recursion.
            return JObject.FromObject(this, serializer);
        }
        public virtual void ReadToken(JToken token, JsonSerializer serializer)
        {
            if (token == null) return;
            using (var jr = token.CreateReader())
                serializer.Populate(jr, this);
        }

        public static void ReadSnap(string snapshot, Saveable saveable) =>
    JsonConvert.PopulateObject(snapshot, saveable, DefaultSerializer);
        public static void WriteSnap(Saveable saveable, out string snapshot) =>
            snapshot = JsonConvert.SerializeObject(saveable, DefaultSerializer);

        public static JToken PruneDefaults(JToken current, JToken defaults)
        {
            if (JToken.DeepEquals(current, defaults))
                return null; // nothing different at this node

            if (current == null) return null;
            if (defaults == null) return current.DeepClone();

            if (current.Type != defaults.Type)
                return current.DeepClone();

            switch (current.Type)
            {
                case JTokenType.Object:
                {
                    var curObj = (JObject)current;
                    var defObj = defaults as JObject ?? new JObject();
                    var outObj = new JObject();
                    foreach (var prop in curObj.Properties())
                    {
                        var defProp = defObj.Property(prop.Name);
                        var prunedChild = PruneDefaults(prop.Value, defProp?.Value);
                        if (prunedChild != null)
                            outObj.Add(prop.Name, prunedChild);
                    }
                    return outObj.HasValues ? outObj : null;
                }
                case JTokenType.Array:
                {
                    // Simple heuristic: if arrays are equal -> prune; if not equal -> keep full current array.
                    var defArr = defaults as JArray;
                    var curArr = current as JArray;
                    if (JToken.DeepEquals(curArr, defArr)) return null;
                    // Optionally implement element-wise pruning here; for now return full current
                    return curArr.DeepClone();
                }
                default:
                    // primitive types -> since not DeepEquals, return current value (replace)
                    return current.DeepClone();
            }
        }

        // Merge a delta token onto a base token (non-destructive: produces merged copy)
        public static JToken ApplyDeltaToBase(JToken baseToken, JToken delta)
        {
            if (delta == null) return baseToken.DeepClone();
            if (baseToken == null) return delta.DeepClone();

            if (delta.Type != JTokenType.Object || baseToken.Type != JTokenType.Object)
                return delta.DeepClone();

            var baseObj = (JObject)baseToken.DeepClone();
            var deltaObj = (JObject)delta;
            foreach (var prop in deltaObj.Properties())
            {
                baseObj[prop.Name] = ApplyDeltaToBase(baseObj[prop.Name], prop.Value);
            }
            return baseObj;
        }


        public static readonly JsonSerializerSettings DefaultSerializer = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            Converters =
            {
                PrimarySaveableConverter
            }
        };
        public static readonly SaveableJsonConverter PrimarySaveableConverter = new();

        public sealed class SaveableJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(Saveable).IsAssignableFrom(objectType);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                var sv = (Saveable)value;

                // Get settings for this concrete type and build a local JsonSerializer
                var settings = sv.Serializer ?? DefaultSerializer;
                var local = JsonSerializer.Create(settings);

                // Avoid recursion: remove SaveableJsonConverter instances from the local serializer
                for (int i = local.Converters.Count - 1; i >= 0; --i)
                    if (local.Converters[i] is SaveableJsonConverter)
                        local.Converters.RemoveAt(i);

                // Let the instance produce a JToken using the local serializer and write it
                var token = sv.WriteToken(local) ?? JValue.CreateNull();
                token.WriteTo(writer, local.Converters.ToArray());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var token = JToken.Load(reader);

                // create or reuse instance
                Saveable instance = existingValue as Saveable;
                // create instance even if ctor non-public
                instance ??= (Saveable)Activator.CreateInstance(objectType, nonPublic: true);

                // Use the instance's desired settings
                var settings = instance.Serializer ?? DefaultSerializer;
                var local = JsonSerializer.Create(settings);

                // Avoid recursion
                for (int i = local.Converters.Count - 1; i >= 0; --i)
                    if (local.Converters[i] is SaveableJsonConverter)
                        local.Converters.RemoveAt(i);

                instance.ReadToken(token, local);
                return instance;
            }

            public override bool CanRead => true;
            public override bool CanWrite => true;
        }
    }
}
