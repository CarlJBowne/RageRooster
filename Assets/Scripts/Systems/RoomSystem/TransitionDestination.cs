namespace RageRooster.RoomSystem
{
    public struct TransitionDestination
    {
        public AreaAsset area;
        public RoomAsset room;
        public SpawnPoint spawn;

        public int spawnID;

        public static TransitionDestination Default() => new()
        {
            area = null,
            room = null,
            spawn = null,
            spawnID = -1
        };
        public bool IsValid() => area != null && room != null && room.area == area && (spawnID >= 0 || spawnID == -1);


        /// <summary>
        /// This Constructor is for use when reading serialized data from a save file or similar.
        /// </summary>
        public TransitionDestination(string areaName, int roomID = 0, int spawnID = 0)
        {
            area = AreaRegistry.GetArea(areaName);
            if (area == null) throw new System.Exception("Invalid name does not belong to any area.");
            if (roomID < 0 || roomID >= area.rooms.Count) throw new System.Exception("Invalid roomID does not belong to the specified area.");
            room = area.rooms[roomID];
            this.spawnID = spawnID;
            spawn = null;
        }

        /// <summary>
        /// This Constructor is for use when a developer has pressed play when a Room Scene was open in the editor.
        /// </summary>
        public TransitionDestination(RoomRoot areaRoot)
        {
            area = areaRoot.asset.area;
            room = areaRoot.asset;
            spawnID = 0;
            spawn = null;
            // NOTE: Add Debug Save File Checking Later.
        }
        /// <summary>
        /// This Constructor is for use when a developer has pressed play when a Area Scene was open in the editor.
        /// </summary>
        public TransitionDestination(AreaRoot areaRoot)
        {
            area = areaRoot.asset;
            room = areaRoot.asset.rooms[0];
            spawnID = 0;
            spawn = null;
            // NOTE: Add Debug Save File Checking Later.
        }
        /// <summary>
        /// This Constructor is for use when a developer has pressed "Play from here" on a Spawn Point.
        /// </summary>
        public TransitionDestination(SpawnPoint spawn)
        {
            area = spawn.root.asset.area;
            room = spawn.root.asset;
            this.spawn = spawn; // This is most likely going to end up nulled at some point during the loading process.
            spawnID = spawn.ID;
        }
        /// <summary>
        /// This Constructor is for when a developer begins directly from the Gameplay Scene. Either defaults to very first spawn in the game or reads the Debug Save File.
        /// </summary>
        /// <param name="gameplay"></param>
        public static TransitionDestination GameplaySceneStart()
        {
            if (false) // Replace false with a check for a debug save file.
            {
                
            }
            else
            {
                TransitionDestination dest = new();
                dest.area = AreaRegistry.GetFirstArea();
                dest.room = dest.area.rooms[0];
                dest.spawnID = 0;
                dest.spawn = null;
                return dest;
            }
        }

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