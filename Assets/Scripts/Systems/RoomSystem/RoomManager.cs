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

            if (dest.areaAsset == null) 
                dest.areaAsset = AreaRegistry.GetArea(dest.areaName);

            yield return dest.areaAsset.LoadArea();
            AreaRoot areaRoot = dest.areaAsset.root;

            if (dest.roomAsset == null) 
                dest.roomAsset = dest.areaAsset.rooms[dest.roomID];

            yield return dest.roomAsset.PrepEnter();
            RoomRoot roomRoot = dest.roomAsset.root;
            EnterRoom(dest.roomAsset);

            if (dest.spawnPoint == null) 
                dest.spawnPoint = roomRoot.spawns[dest.spawnID];
            dest.spawnPoint.SpawnPlayerAt();

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