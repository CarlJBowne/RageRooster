using RageRooster.Systems.SaveSystem;
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

        public static Destination destination;
        public static bool loading;

        public static IEnumerator Transition(Destination destination, bool forceFullTransition = false)
        {
            RoomManager.destination = destination;
            return Transition(forceFullTransition);
        }
        public static IEnumerator Transition(bool forceFullTransition = false)
        {
            if (!destination.IsValid()) throw new System.Exception("No valid destination.");

            Player.SetActive(false);
            yield return null;
            loading = true;
            OverlayLoading.ShowIfLong();

            if (destination.area == null) destination.area = destination.room.area;

            bool fullTransition = currentArea != destination.area || forceFullTransition;

            if (fullTransition)
            {
                if (currentArea != null) yield return currentArea.UnloadArea();
                currentArea = null;
                currentRoom = null;
                currentArea = destination.area;
                yield return currentArea.LoadArea();
            }

            yield return destination.room.PrepEnter();
            EnterRoom(destination.room);

            if (destination.spawn == null)
                destination.spawn = currentRoom.root.spawns[destination.spawnID];
            destination.spawn.SpawnPlayerAt();

            if (fullTransition)
            {
                SaveFile.Current.location = destination;
                SaveFile.DeathReloadData.location = destination;
            }

            foreach (RoomAsset room in currentArea.rooms)
                yield return room.PrepSurrounding();

            loading = false;
            OverlayLoading.SetVisible(false);
            Player.SetActive(true);

            destination = Destination.Default;
        }


        public static void EnterRoom(RoomAsset nextRoom)
        {
            if(currentRoom != null) currentRoom._Exit();
            currentRoom = nextRoom;
            currentRoom._Enter();
        }
    }
}