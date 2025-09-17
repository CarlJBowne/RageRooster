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

        public static Destination respawnDestination = Destination.Default;
        public static Destination deathRespawnDestination = Destination.Default;



        public static IEnumerator TransitionOut()
        {
            yield return currentArea.UnloadArea();
            currentArea = null;
            currentRoom = null;
        }
        public static Destination transitionDestination;
        public static IEnumerator TransitionIn()
        {
            if (!transitionDestination.IsValid()) throw new System.Exception("No valid destination.");

            Player.SetActive(false);
            OverlayLoading.SetVisible(true);

            yield return null;

            if (transitionDestination.area == null) transitionDestination.area = transitionDestination.room.area;

            currentArea = transitionDestination.area;
            yield return currentArea.LoadArea();

            yield return transitionDestination.room.PrepEnter();
            EnterRoom(transitionDestination.room);

            if (transitionDestination.spawn == null) 
                transitionDestination.spawn = currentRoom.root.spawns[transitionDestination.spawnID];
            transitionDestination.spawn.SpawnPlayerAt();

            foreach (RoomAsset room in currentArea.rooms) 
                yield return room.PrepSurrounding();

            yield return new WaitForSecondsRealtime(0.5f);

            OverlayLoading.SetVisible(false);
            Player.SetActive(true);
        }


        public static void EnterRoom(RoomAsset nextRoom)
        {
            if(currentRoom != null) currentRoom._Exit();
            currentRoom = nextRoom;
            currentRoom._Enter();
        }
    }
}