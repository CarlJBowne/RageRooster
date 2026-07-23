using RageRooster.RoomSystem;
using RageRooster.Systems;
using Utilities.ObjectPooling;
using RageRooster.Systems.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.Singletons;
using SLS.MenuCore;
using SLS.MenuCore;

namespace RageRooster.RoomSystem
{
    /// <summary>
    /// Global Gameplay System for managing Room transitions, current Room/Area tracking, and related functionality.
    /// </summary>
    public class RoomManager : Singleton.MonoBehaviour<RoomManager>
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
        public static bool CurrentlyTransitioning;


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

        [RuntimeInitializeOnLoadMethod]
        static void InitStaticServices()
        {
            Services.RoomManager.CurrentlyTransitioning = new(() => CurrentlyTransitioning);
            Services.RoomManager.TransitionStyle = new()
            {
                Setter = value => TransitionStyle = value
            };
        }


        /// <summary>
        /// Begins a Room transition to the specified <see cref="Destination"/>.
        /// </summary>
        public static void StartTransition(Destination destination = default)
            => Transition(destination).Begin(Overlay.OverALL);

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

            if (FadeOutRoutine == Overlay.UnderHUD.FadeAlpha(1, 0.5f) 
                && FadeInRoutine == Overlay.BetweenUI.FadeAlpha(0, 0.5f) && fullTransition)
            {
                FadeOutRoutine = Overlay.BetweenUI.FadeAlpha(1, 0.5f);
                FadeInRoutine = Overlay.BetweenUI.FadeAlpha(0, 0.5f);
            }

            if (fullTransition) Music.FadeOutBothMusic();

            PreFadeOutAction?.Invoke();
            if (FadeOutRoutine != null)
                yield return FadeOutRoutine;
            PostFadeOutAction?.Invoke();


            Player.ActivityState = Player.ActivityStates.Invisible;
            yield return null;
            CurrentlyTransitioning = true;
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

            CurrentlyTransitioning = false;
            OverlayLoading.SetVisible(false);
            Player.ActivityState = Player.ActivityStates.Active;


            try { if (fullTransition) Music.BeginPrimaryMusic(currentArea.music); }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
            }

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
            FadeOutRoutine = Overlay.UnderHUD.FadeAlpha(1, 0.5f);
            FadeInRoutine = Overlay.UnderHUD.FadeAlpha(0, 0.5f);
            forceFullTransition = false;
        }

        /// <summary>
        /// Tell the system that the player has officially entered a given room. (Does not handle transitions.)
        /// </summary>
        /// <param name="nextRoom">The target room to enter</param>
        public static void EnterRoom(RoomAsset nextRoom)
        {
            if (currentRoom == nextRoom) return;
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
            public IEnumerator FadeOutRoutine = Overlay.UnderHUD.FadeAlpha(1, 0.5f);
            public Action PostFadeOutAction = null;
            public IEnumerator MidTransitionRoutine = null;
            public Action PreFadeInAction = null;
            public IEnumerator FadeInRoutine = Overlay.UnderHUD.FadeAlpha(0, 0.5f);
            public Action PostFadeInAction = null;

            public static implicit operator TransitionData(Services.RoomManager.TransitionData input)
            {
                TransitionData res = new();
                res.forceFullTransition = input.forceFullTransition;
                res.PreFadeOutAction = input.PreFadeOutAction;
                res.FadeOutRoutine = input.FadeOutRoutine;
                res.PostFadeOutAction = input.PostFadeOutAction;
                res.MidTransitionRoutine = input.MidTransitionRoutine;
                res.PreFadeInAction = input.PreFadeInAction;
                res.FadeInRoutine = input.FadeInRoutine;
                res.PostFadeInAction = input.PostFadeInAction;

                return res;
            }
        }
    }
}