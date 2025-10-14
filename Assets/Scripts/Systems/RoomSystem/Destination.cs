using Newtonsoft.Json.Linq;
using RageRooster.Systems.SaveSystem;
using System;
using System.Collections.Generic;
using static RageRooster.RoomSystem.RoomAsset;

namespace RageRooster.RoomSystem
{
    /// <summary>
    /// Represents a specific Destination within the game world that can be fed into the <see cref="RoomManager"/>, defined with a <see cref="RoomAsset"/> and a <see cref="SpawnPoint"/> ID. <br/>
    /// A Serialized equivalent, <see cref="DestinationSerial"/>, is also provided for easy saving/loading of Destinations. <br/>
    /// </summary>
    [System.Serializable]
    public struct Destination
    {
        /// <summary>
        /// The target Room of the destination, defined using a <see cref="RoomAsset"/>. <br/>
        /// The appropriate <see cref="AreaAsset"/> can be accessed through <see cref="area"/>.
        /// </summary>
        public RoomAsset room;
        /// <summary>
        /// The ID of the <see cref="SpawnPoint"/> within the Room to spawn at.
        /// </summary>
        public int spawnID;
        /// <summary>
        /// Quick access to the <see cref="AreaAsset"/> the target Room belongs to.
        /// </summary>
        public AreaAsset area => room.area;

        public static Destination Null => new()
        {
            room = null,
            spawnID = -1
        };

        public static Destination StartingDefault() => new()
        {
            room = AreaRegistry.GetAll()[0].rooms[0],
            spawnID = 0
        };


        public bool IsValid() => room != null && spawnID >= 0;
        public bool IsNull() => room == null && spawnID == -1;

        public static bool operator ==(Destination a, Destination b) => a.area == b.area && a.room == b.room && a.spawnID == b.spawnID;
        public static bool operator !=(Destination a, Destination b) => !(a.area == b.area && a.room == b.room && a.spawnID == b.spawnID);

        public static implicit operator bool(Destination destination) => destination.IsValid();

        public override bool Equals(object obj) => obj is Destination destination && EqualityComparer<AreaAsset>.Default.Equals(area, destination.area) && EqualityComparer<RoomAsset>.Default.Equals(room, destination.room) && spawnID == destination.spawnID;
        public override int GetHashCode() => HashCode.Combine(area, room, spawnID);







        public static implicit operator Destination(RoomAsset room) => new()
        {
            room = room,
            spawnID = -1
        };
        public static implicit operator Destination(SpawnPoint spawn) => new()
        {
            room = spawn.root.asset,
            spawnID = spawn.ID
        };



        //Possibly Unnecessary Constructors, real constructers will be created on a necessary case basis to ensure no willy-nilly usage of potentially malformed Destinations.
        /* 
        public RoomDestination(AreaAsset area, RoomAsset room, SpawnPoint spawn)
        {
            if(area == null || room == null || room.area != area || spawn == null) 
                throw new System.NullReferenceException("Invalid parameters passed to RoomDestination constructor.");
            areaAsset = area;
            roomAsset = room;
            spawnPoint = spawn;
            spawnID = -2;
        }

        public RoomDestination(AreaAsset area, RoomAsset room, int spawnID = 0)
        {
            if(area == null || room == null || room.area != area) 
                throw new System.NullReferenceException("Invalid parameters passed to RoomDestination constructor.");
            areaAsset = area;
            roomAsset = room;
            this.spawnID = spawnID;
            spawnID = -2;
            spawnPoint = null;
        }

        public RoomDestination(AreaAsset area, int roomID = 0, int spawnID = 0)
        {
            if(area == null) throw new System.NullReferenceException("Invalid parameters passed to RoomDestination constructor.");
            areaAsset = area;
            roomAsset = area.rooms[roomID];
            this.spawnID = spawnID;
            spawnPoint = null;
        }

        public RoomDestination(RoomAsset room, int spawnID = 0)
        {
            if(room == null) throw new System.NullReferenceException("Invalid parameters passed to RoomDestination constructor.");
            areaAsset = room.area;
            roomAsset = room;
            this.spawnID = spawnID;
            spawnPoint = null;
        }

        public RoomDestination(string areaName, int roomID = 0, int spawnID = 0)
        {
            areaAsset = AreaRegistry.GetArea(areaName);
            if(areaAsset == null) throw new System.Exception("Invalid name does not belong to any area.");
            roomAsset = areaAsset.rooms[roomID];
            this.spawnID = spawnID;
            spawnPoint = null;
        }

        public RoomDestination(SpawnPoint spawn)
        {
            if(spawn == null) throw new System.NullReferenceException("Invalid parameters passed to RoomDestination constructor.");
            areaAsset = spawn.root.asset.area;
            roomAsset = spawn.root.asset;
            spawnPoint = spawn;
            spawnID = -2;
        }
        */
    }

    /// <summary>
    /// A serialized version of <see cref="Destination"/> using basic data types that can be easily saved/loaded. <br/>
    /// </summary>
    public struct DestinationSerial
    {
        /// <summary>
        /// The Display name of the Area. Used to look up the <see cref="AreaAsset"/> through the <see cref="AreaRegistry"/>.
        /// </summary>
        public string areaName;
        /// <summary>
        /// The ID of the Room within the Area's list. Used to look up the <see cref="RoomAsset"/> within the <see cref="AreaAsset"/>.
        /// </summary>
        public int roomID;
        /// <summary>
        /// The <see cref="SpawnPoint"/> ID within the Room to spawn at.
        /// </summary>
        public int spawnID;

        public static implicit operator JToken(DestinationSerial serial) => new JObject
        {
            ["area"] = serial.areaName,
            ["roomID"] = serial.roomID,
            ["spawnID"] = serial.spawnID
        };
        public static implicit operator DestinationSerial(JToken Data) => new()
        {
            areaName = (string)Data["area"],
            roomID = (int)Data["roomID"],
            spawnID = (int)Data["spawnID"]
        };

        /// <summary>
        /// Converts this Destination into the serializable format equivalent.
        /// </summary>
        /// <returns></returns>
        public static explicit operator DestinationSerial(Destination destination) => new()
        {
            areaName = destination.area.name,
            roomID = destination.area.rooms.IndexOf(destination.room),
            spawnID = destination.spawnID
        };

        /// <summary>
        /// Converts this Serial Destination back into the runtime Asset-based equivalent.
        /// </summary>
        public static explicit operator Destination(DestinationSerial input)
        {
            AreaAsset area = AreaRegistry.GetArea(input.areaName);
            if (area == null) throw new System.Exception("Invalid name does not belong to any area.");
            if (input.roomID < 0 || input.roomID >= area.rooms.Count) throw new System.Exception("Invalid roomID does not belong to the specified area.");
            return new Destination
            {
                room = area.rooms[input.roomID],
                spawnID = input.spawnID
            };
        }
    }
}