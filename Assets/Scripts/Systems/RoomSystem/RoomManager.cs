using RageRooster.Systems;
using RageRooster.Systems.ObjectPool;
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

        public static bool currentlyTransitioning;

        public static Destination destination;
        public static bool forceFullTransition = false;

        public static IEnumerator FadeOutRoutine;
        public static IEnumerator FadeInRoutine;



        public static IEnumerator Transition(Destination destination = default, bool forceFullTransition = false)
        {
            if(destination.IsValid()) RoomManager.destination = destination;
            RoomManager.forceFullTransition = forceFullTransition;
            return Transition();
        }
        public static IEnumerator Transition()
        {
            if (!destination.IsValid()) throw new System.Exception("No valid destination.");

            bool fullTransition = currentArea != destination.area || forceFullTransition;

            if(fullTransition) Music.FadeOutBothMusic();

            Player.SetActive(false);
            yield return null;
            currentlyTransitioning = true;
            OverlayLoading.ShowIfLong();

            if (fullTransition)
            {
                if (currentArea != null) yield return currentArea.UnloadArea();
                currentArea = null;
                currentRoom = null;
                currentArea = destination.area;
                ObjectPools.UnloadAllPools();
                yield return currentArea.LoadArea();
            }

            yield return destination.room.PrepEnter();
            EnterRoom(destination.room);

            SpawnPoint targetSpawn = currentRoom.root.spawns[destination.spawnID];

            targetSpawn.SpawnPlayerAt();

            if (fullTransition)
            {
                SaveData.Current.location = destination;
                SaveData.DeathReloadData.location = destination;
            }

            foreach (RoomAsset room in currentArea.rooms)
                yield return room.PrepSurrounding();

            currentlyTransitioning = false;
            OverlayLoading.SetVisible(false);
            Player.SetActive(true);

            if (fullTransition) Music.BeginPrimaryMusic(currentArea.music);

            destination = Destination.Null;
        }


        public static void EnterRoom(RoomAsset nextRoom)
        {
            if(currentRoom != null) currentRoom._Exit();
            currentRoom = nextRoom;
            currentRoom._Enter();
        }
    }
}