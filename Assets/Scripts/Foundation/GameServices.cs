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
        public static class Stats
        {
            public static Action<int, int, int> ResetService;
            public static void Reset(int maxHealth, int maxAmmo, int currency) => ResetService?.Invoke(maxHealth, maxAmmo, currency);
        }
    }
}