using Newtonsoft.Json.Linq;
using RageRooster.Core.Save;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.VersionControl;

using static RageRooster.World.IDestination;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using SLS.EditorUtilities.Editor;
#endif

namespace RageRooster.World
{

    /// <summary>
    /// The Final Functional Type of the <see cref="IDestination"/>, holds direct asset references and and integer spawn ID.
    /// </summary>
    public class Destination : IDestination
    {
        public AreaAsset area { get; private set; }
        public RoomAsset room { get; private set; }
        public int spawn { get; private set; }

        public object iArea => area;
        public object iRoom => room;
        public object iSpawn => spawn;

        public Destination()
        {
            area = AreaRegistry.GetArea(0);
            room = area.GetRoom(0);
            spawn = 0;
        }

        public Destination(RoomAsset room, int spawnID)
        {
            this.room = room;
            area = room.area;
            spawn = spawnID;
        }
        public Destination(RoomAsset room, string spawnName)
        {
            this.room = room;
            area = room.area;
            spawn = room.spawnPointNames.IndexOf(spawnName);
        }

        public static implicit operator Destination(DestinationMap source)
        {
            Destination result = new();
            result.area = AreaRegistry.GetArea(source.area);
            result.room = result.area.GetRoom(source.room);
            result.spawn = result.room.spawnPointNames.IndexOf(source.spawn);
            return result;
        }
        public static implicit operator Destination(AreaAsset area)
        {
            Destination result = new();
            result.area = area;
            result.room = result.area.GetRoom(0);
            result.spawn = 0;
            return result;
        }
        public static implicit operator Destination(RoomAsset room) => new()
        {
            area = room.area,
            room = room,
            spawn = 0
        };

        public static implicit operator DestinationMap(Destination s) => new()
        {
            area = s.area.name,
            room = s.room.name,
            spawn = s.room.spawnPointNames[s.spawn]
        };

        /// <summary> Validity Check </summary>
        public static implicit operator bool(Destination asset) => asset != null && asset.area != null && asset.room != null && asset.room.area == asset.area && asset.spawn > 1 && asset.spawn < asset.room.spawnPointNames.Count;
    }

}