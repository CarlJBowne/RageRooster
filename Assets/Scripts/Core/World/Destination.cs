using Newtonsoft.Json.Linq;
using RageRooster.Core.Save;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.VersionControl;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using SLS.EditorUtilities.Editor;
#endif

namespace RageRooster.Core.World
{

    /// <summary>
    /// The Final Functional Type of the <see cref="IDestination"/>, holds direct asset references and and integer spawn ID.
    /// </summary>
    public class Destination
    {
        public AreaAsset area { get; private set; }
        public RoomAsset room { get; private set; }
        public int spawn { get; private set; }

        public Destination()
        {
            area = AreaRegistry.All[0];
            room = area[0];
            spawn = 0;
        }

        public Destination(RoomAsset room, int spawnID)
        {
            this.room = room;
            area = room.area;
            spawn = spawnID;
        }

        public static implicit operator Destination(AreaAsset area)
        {
            Destination result = new();
            result.area = area;
            result.room = result.area[0];
            result.spawn = 0;
            return result;
        }
        public static implicit operator Destination(RoomAsset room) => new()
        {
            area = room.area,
            room = room,
            spawn = 0
        };
        public static explicit operator Destination(JToken source)
        {
            Destination result = new();

            if (source is JArray a)
            {
                result.area = AreaRegistry.Get[(string)a[0]];
                result.room = result.area[(string)a[1]];
                result.spawn = result.room.spawnPointNames.IndexOf((string)a[2]);
            }
            else if (source is JObject o)
            {
                result.area = AreaRegistry.Get[(string)o["area"]];
                result.room = result.area[(int)o["roomID"]];
                result.spawn = (int)o["spawnID"];
            }
            else throw new Exception();

            return result;
        }
        public static explicit operator JArray(Destination source)
        {
            JArray res = new()
            {
                source.area.name,
                source.room.name,
                source.room.spawnPointNames[source.spawn]
            };

            return res;
        }

        /// <summary> Validity Check </summary>
        public static implicit operator bool(Destination asset) => asset != null && asset.area != null && asset.room != null && asset.room.area == asset.area && asset.spawn > 1 && asset.spawn < asset.room.spawnPointNames.Count;

        static Destination() => Default = new Destination();
        public static Destination Default { get; private set; }
    }

}