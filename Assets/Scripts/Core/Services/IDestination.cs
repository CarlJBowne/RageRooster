using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor.VersionControl;

namespace RageRooster.World
{
    public interface IDestination
    {
        public object iArea { get; }
        public object iSpawn { get; }
        public object iRoom { get; }

        public static string[] AllAreas;
    }

    /// <summary>
    /// A Type of <see cref="IDestination"/> that holds string IDs.
    /// </summary>
    public class DestinationMap : IDestination
    {
        public string area;
        public string room;
        public string spawn;

        public object iArea => area;
        public object iRoom => room;
        public object iSpawn => spawn;

        public static implicit operator DestinationMap(JToken i)
        {
            DestinationMap r = new();

            if (i is JArray array)
            {
                r.area = array[0].ToObject<string>();
                r.room = array[1].ToObject<string>();
                r.spawn = array[2].ToObject<string>();
            }
            //else if (i is JObject obj)
            //{
            //    r.area = obj["area"].ToObject<string>();
            //    r.room = GetArea(r.area).rooms[obj["roomID"].ToObject<int>()].name;
            //    r.room = GetArea(r.area).rooms[obj["spawnID"].ToObject<int>()].spawnPointNames[];
            //} //Backwards Compatibility with Pre-Segmentation Verison. Consider messing around with later.
            else return Default;

            return r;
        }
        public static implicit operator JToken(DestinationMap i) => new JArray(i.area, i.room, i.spawn);

        public static DestinationMap Default;
    }
}
