using RageRooster.RoomSystem;
using RageRooster.Systems;
using RageRooster.Systems.ObjectPool;
using RageRooster.Systems.SaveSystem;
using SLS.ISingleton;
using System;
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

        public static Action PreFadeOutAction;
        public static IEnumerator FadeOutRoutine;
        public static Action PostFadeOutAction;
        public static IEnumerator MidTransitionRoutine;
        public static Action PreFadeInAction;
        public static IEnumerator FadeInRoutine;
        public static Action PostFadeInAction;

        public static void StartTransition(Destination destination = default)
            => Transition(destination).Begin(Overlay.OverMenus);

        public static IEnumerator Transition(Destination destination = default)
        {
            if (!destination.IsValid()) destination = RoomManager.destination;
            if (!destination.IsValid()) destination = SaveData.Current.location;
            if (!destination.IsValid()) destination = SaveData.DeathReloadData.location;
            if (!destination.IsValid()) destination = Destination.StartingDefault();

            bool fullTransition = currentArea != destination.area || forceFullTransition;

            if (FadeOutRoutine == Overlay.OverGameplay.BasicFadeOutWait(0.5f) && FadeInRoutine == Overlay.OverGameplay.BasicFadeInWait(0.5f) && fullTransition)
            {
                FadeOutRoutine = Overlay.OverHUD.BasicFadeOutWait(0.5f);
                FadeInRoutine = Overlay.OverHUD.BasicFadeInWait(0.5f);
            }

            if (fullTransition) Music.FadeOutBothMusic();

            PreFadeOutAction?.Invoke();
            if (FadeOutRoutine != null)
                yield return FadeOutRoutine;
            PostFadeOutAction?.Invoke();


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

            yield return MidTransitionRoutine;

            currentlyTransitioning = false;
            OverlayLoading.SetVisible(false);
            Player.SetActive(true);


            if (fullTransition) Music.BeginPrimaryMusic(currentArea.music);

            PreFadeInAction?.Invoke();
            if (FadeInRoutine != null)
                yield return FadeInRoutine;
            PostFadeInAction?.Invoke();

            ResetTransitionData();
        }

        public static void ResetTransitionData(bool resetDestination = true)
        {
            if (resetDestination) destination = Destination.Null;
            PreFadeInAction = null;
            PostFadeInAction = null;
            PreFadeOutAction = null;
            PostFadeOutAction = null;
            FadeOutRoutine = Overlay.OverGameplay.BasicFadeOutWait(0.5f);
            FadeInRoutine = Overlay.OverGameplay.BasicFadeInWait(0.5f);
            forceFullTransition = false;
        }


        public static void EnterRoom(RoomAsset nextRoom)
        {
            if (currentRoom != null) currentRoom._Exit();
            currentRoom = nextRoom;
            currentRoom._Enter();
        }


        public static TransitionData TransitionStyle
        {
            set
            {
                forceFullTransition = value.forceFullTransition;
                PreFadeOutAction = value.PreFadeOutAction;
                FadeOutRoutine = value.FadeOutRoutine;
                PostFadeOutAction = value.PostFadeOutAction;
                MidTransitionRoutine = value.MidTransitionRoutine;
                PreFadeInAction = value.PreFadeInAction;
                FadeInRoutine = value.FadeInRoutine;
                PostFadeInAction = value.PostFadeOutAction;
            }
        }

        /// <summary>
        /// This class is purely for the purposes of easy-optional-assignment-via-contructor of the various fields related to how a Transition should be enacted. See <see cref="RoomManager.TransitionStyle"/>
        /// </summary>
        public class TransitionData
        {
            public bool forceFullTransition = false;
            public Action PreFadeOutAction = null;
            public IEnumerator FadeOutRoutine = Overlay.OverGameplay.BasicFadeOutWait(0.5f);
            public Action PostFadeOutAction = null;
            public IEnumerator MidTransitionRoutine = null;
            public Action PreFadeInAction = null;
            public IEnumerator FadeInRoutine = Overlay.OverGameplay.BasicFadeInWait(0.5f);
            public Action PostFadeInAction = null;

        }
    }
}