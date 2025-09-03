using SLS.ISingleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    public class RoomManager : SingletonMonoBasic<RoomManager>
    {
        public static AreaAsset currentArea { get; private set; }
        public static RoomAsset currentRoom { get; private set; }




        public static IEnumerator ExitArea()
        {
            foreach (var room in currentArea.rooms)
                yield return room.CompleteUnload();
            yield return currentArea.UnloadArea();
            currentArea = null;
            currentRoom = null;
        }
        public static IEnumerator EnterArea(RoomDestination dest)
        {
            yield return null;

            yield return dest.room.PrepEnter();
            RoomRoot roomRoot = dest.room.root;
            EnterRoom(dest.room);

            if (dest.spawn == null) 
                dest.spawn = roomRoot.spawns[dest.spawnID];
            dest.spawn.SpawnPlayerAt();

            foreach (RoomAsset room in currentArea.rooms)
            {
                if (room == currentRoom) continue;
                yield return room.PrepSurrounding();
            }
        }


        public static void EnterRoom(RoomAsset nextRoom)
        {
            if(currentRoom != null) currentRoom._Exit();
            currentRoom = nextRoom;
            currentRoom._Enter();
        }
    }
}