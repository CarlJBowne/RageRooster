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




        public static IEnumerator TransitionOut()
        {
            foreach (var room in currentArea.rooms)
                yield return room.CompleteUnload();
            yield return currentArea.UnloadArea();
            currentArea = null;
            currentRoom = null;
        }
        public static TransitionDestination transitionDestination;
        public static IEnumerator TransitionIn()
        {
            if (!transitionDestination.IsValid()) throw new System.Exception("No valid destination.");

            yield return null;

            currentArea = transitionDestination.area;

            yield return transitionDestination.room.PrepEnter();
            EnterRoom(transitionDestination.room);

            if (transitionDestination.spawn == null) 
                transitionDestination.spawn = currentRoom.root.spawns[transitionDestination.spawnID];
            transitionDestination.spawn.SpawnPlayerAt();

            foreach (RoomAsset room in currentArea.rooms) 
                yield return room.PrepSurrounding();
        }


        public static void EnterRoom(RoomAsset nextRoom)
        {
            if(currentRoom != null) currentRoom._Exit();
            currentRoom = nextRoom;
            currentRoom._Enter();
        }
    }
}