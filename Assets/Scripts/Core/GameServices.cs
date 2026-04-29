using System;
using System.Collections;
using UnityEngine;

namespace Services
{
    public static class RoomManager
    {
        public static Service<bool> CurrentlyTransitioning;
        public static GetterSetterService<TransitionData> TransitionStyle;

        /// <summary>
        /// This class is purely for the purposes of easy-optional-assignment-via-contructor of the various fields related to how a Transition should be enacted. See <see cref="RoomManager.TransitionStyle"/>
        /// </summary>
        public class TransitionData
        {
            public bool forceFullTransition = false;
            public Action PreFadeOutAction = null;
            public IEnumerator FadeOutRoutine = null;
            public Action PostFadeOutAction = null;
            public IEnumerator MidTransitionRoutine = null;
            public Action PreFadeInAction = null;
            public IEnumerator FadeInRoutine = null;
            public Action PostFadeInAction = null;

        }
    }
    public static class Gameplay
    {
        public enum GameStates
        {
            Null = -1,
            Active = 0,
            Paused = 1,
            Processing = 2,
        }

        public static GetterSetterService<GameStates> GameState;
        public static Action ReloadSave;
        public static Action Respawn;
        public static Action EndGame;
    }

    public static class Player
    {
        /*
        public static Service<Vector3> Position;
        public static Action<Vector3> SetPosition;
        public static Service<CapsuleCollider> Collider;
        public static Service<GameObject> GameObject;
        public static Service<Transform> Transform;
        public static Action MovingUpdateAction;
        public static Action<Vector3, float?> InstantMove;

        /// <summary>
        /// An enum representing states of activity for the Player. 
        /// </summary>
        public enum ActivityStates
        {
            /// <summary> The <see cref="Player"/> has not been loaded in as <see cref="Gameplay"/> is not active. </summary>
            Null = -1,
            /// <summary> The <see cref="Player"/> is active and controlled by the player. </summary>
            Active = 0,
            /// <summary> The <see cref="Player"/> is paused in place, still visible, but not moving. </summary>
            Paused = 1,
            /// <summary> The player is in the dying animation. </summary>
            Dying = 2,
            /// <summary> The player is outside of the visibly active scene and thus unrendered.</summary>
            Invisible = 3,
            /// <summary> The game is in a cutscene state and all active logic on the <see cref="Player"/> has been paused. </summary>
            Cutscene = 4,
            /// <summary> 
            /// The game is currently in a Minigame state where the player's default behavior is not present. 
            /// <br/> Minigames where the player moves and acts as normal may be implemented in a different way.
            /// </summary>
            Minigame = 5,
        }
        public static GetterSetterService<ActivityStates> ActivityStateService;
        public static ActivityStates ActivityState
        {
            get => ActivityStateService;
            set => ActivityStateService.Setter(value);
        }

        public static class Stats
        {
            public static Action<int, int, int> ResetService;
            public static void Reset(int maxHealth, int maxAmmo, int currency) => ResetService?.Invoke(maxHealth, maxAmmo, currency);

            public static Service<int> CurrentHP;
            public static Service<int> CurrentMaxHP;
            public static Service<int> CurrentAmmo;
            public static Service<int> CurrentMaxAmmo;
            public static Service<int> CurrentCurrency;

        }
        */
    }

    
}

