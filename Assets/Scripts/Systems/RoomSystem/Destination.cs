using Newtonsoft.Json.Linq;
using RageRooster.Systems.SaveSystem;
using System;
using System.Collections.Generic;

namespace RageRooster.RoomSystem
{
    public struct Destination
    {
        public AreaAsset area;
        public RoomAsset room;
        public SpawnPoint spawn;

        public int spawnID;

        public static Destination Default => new()
        {
            area = null,
            room = null,
            spawn = null,
            spawnID = -1
        };
        

        /// <summary>
        /// This Constructor is for use when reading deSerialized data from a save file or similar.
        /// </summary>
        public Destination(string areaName, int roomID = 0, int spawnID = 0)
        {
            area = AreaRegistry.GetArea(areaName);
            if (area == null) throw new System.Exception("Invalid name does not belong to any area.");
            if (roomID < 0 || roomID >= area.rooms.Count) throw new System.Exception("Invalid roomID does not belong to the specified area.");
            room = area.rooms[roomID];
            this.spawnID = spawnID;
            spawn = null;
        }

        public JToken Serialize(string name = null) => new JObject
        {
            ["area"] = area.name,
            ["roomID"] = area.rooms.IndexOf(room),
            ["spawnID"] = spawnID
        };
        public static Destination Deserialize(JToken Data) => new((string)Data["area"], (int)Data["roomID"], (int)Data["spawnID"]);


        public bool IsValid() => room != null && (area == null || room.area == area) && (spawnID >= 0 || spawnID == -1);
        public bool IsFullyDefined() => area != null && room != null && room.area == area && (spawnID >= 0 || (spawnID == -1 && spawn != null && spawn.root.asset == room));
        public bool IsDefault() => area == null && room == null && spawn == null && spawnID == -1;

        public static bool operator ==(Destination a, Destination b) => a.area == b.area && a.room == b.room && a.spawnID == b.spawnID;
        public static bool operator !=(Destination a, Destination b) => !(a.area == b.area && a.room == b.room && a.spawnID == b.spawnID);

        public static implicit operator bool(Destination destination) => destination.IsValid();

        public override bool Equals(object obj) => obj is Destination destination && EqualityComparer<AreaAsset>.Default.Equals(area, destination.area) && EqualityComparer<RoomAsset>.Default.Equals(room, destination.room) && EqualityComparer<SpawnPoint>.Default.Equals(spawn, destination.spawn) && spawnID == destination.spawnID;
        public override int GetHashCode() => HashCode.Combine(area, room, spawn, spawnID);

        public static Destination StartingDefault() => new()
        {
            area = AreaRegistry.GetAll()[0],
            room = AreaRegistry.GetAll()[0].rooms[0],
            spawnID = 0,
            spawn = null
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
}