using RageRooster.RoomSystem;
using RageRooster.Systems;
using RageRooster.Systems.ObjectPooling;
using RageRooster.Systems.SaveSystem;
using SLS.ISingleton;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    /// <summary>
    /// Global Gameplay System for managing Room transitions, current Room/Area tracking, and related functionality.
    /// </summary>
    public class RoomManager : SingletonMonoBasic<RoomManager>
    {
        /// <summary>
        /// The Currently active Area in the game world. <br/>
        /// </summary>
        public static AreaAsset currentArea { get; private set; }
        /// <summary>
        /// The Room the player is currently located in. <br/>
        /// </summary>
        public static RoomAsset currentRoom { get; private set; }

        /// <summary>
        /// If the game is currently in the process of transitioning between Rooms/Areas. <br/>
        /// </summary>
        public static bool currentlyTransitioning;

        /// <summary>
        /// The target Destination for the next Room transition. <br/>
        /// </summary>
        public static Destination destination;
        /// <summary>
        /// Manual override to force a full Deload & Load transition even if too/from the same area or room.
        /// </summary>
        public static bool forceFullTransition = false;

        /// <summary>
        /// A callback Action invoked before the <see cref="FadeOutRoutine"/> begins.
        /// </summary>
        public static Action PreFadeOutAction;
        /// <summary>
        /// The <see cref="IEnumerator"/> routine that performs the Fade Out animation.
        /// </summary>
        public static IEnumerator FadeOutRoutine;
        /// <summary>
        /// A callback Action invoked after the <see cref="FadeOutRoutine"/> completes.
        /// </summary>
        public static Action PostFadeOutAction;
        /// <summary>
        /// An <see cref="IEnumerator"/> routine that runs in the middle of the transition, after unloading/loading but before Fade In.
        /// </summary>
        public static IEnumerator MidTransitionRoutine;
        /// <summary>
        /// A callback Action invoked before the <see cref="FadeInRoutine"/> begins and after the <see cref="MidTransitionRoutine"/>.
        /// </summary>
        public static Action PreFadeInAction;
        /// <summary>
        /// The <see cref="IEnumerator"/> routine that performs the Fade In animation.
        /// </summary>
        public static IEnumerator FadeInRoutine;
        /// <summary>
        /// A callback Action invoked after the <see cref="FadeInRoutine"/> completes.
        /// </summary>
        public static Action PostFadeInAction;

        /// <summary>
        /// Begins a Room transition to the specified <see cref="Destination"/>.
        /// </summary>
        public static void StartTransition(Destination destination = default)
            => Transition(destination).Begin(Overlay.OverMenus);

        /// <summary>
        /// The central Transition Routine run when the player transitions between Rooms/Areas.
        /// </summary>
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


            Player.ActivityState = Player.ActivityStates.Invisible;
            yield return null;
            currentlyTransitioning = true;
            OverlayLoading.ShowIfLong();

            if (fullTransition)
            {
                if (currentArea != null) yield return currentArea.UnloadArea();
                currentArea = null;
                currentRoom = null;
                currentArea = destination.area;
                GlobalPool.UnloadAllPools();
                yield return currentArea.LoadArea();
            }

            yield return destination.room.PrepEnter();
            EnterRoom(destination.room);

            SpawnPoint targetSpawn = currentRoom.root.Spawns[destination.spawnID];

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
            Player.ActivityState = Player.ActivityStates.Active;


            if (fullTransition) Music.BeginPrimaryMusic(currentArea.music);

            PreFadeInAction?.Invoke();
            if (FadeInRoutine != null)
                yield return FadeInRoutine;
            PostFadeInAction?.Invoke();

            ResetTransitionData();
        }

        /// <summary>
        /// Resets all Transition-related data to default values.
        /// </summary>
        /// <param name="resetDestination"></param>
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

        /// <summary>
        /// Tell the system that the player has officially entered a given room. (Does not handle transitions.)
        /// </summary>
        /// <param name="nextRoom">The target room to enter</param>
        public static void EnterRoom(RoomAsset nextRoom)
        {
            if (currentRoom != null) currentRoom._Exit();
            currentRoom = nextRoom;
            currentRoom._Enter();
        }

        /// <summary>
        /// Set-Only property to quickly and succinctly set or not set all optional Transition-related data in one go.
        /// </summary>
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