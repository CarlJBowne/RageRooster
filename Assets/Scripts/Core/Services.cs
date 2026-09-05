using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RageRooster.Core;
using RageRooster.World;
using UnityEngine;

namespace RageRooster
{
    /// <summary>
    /// Static Services available across the project. 
    /// <br/> Add "using static RageRooster.Services;" to gain instant access to all services.
    /// </summary>
    public class Services
    {
        #region Main Services
        public static IPlayer Player
        {
            get;
            set;
        }
        public static IMusicService Music;
        #endregion

        #region Static Services

        public static class UI
        {
            public static bool canPause;
            public static Action<bool> SetPause;
            public static Action<bool> OnPause;
            public static Action<string> ShowHint;
            public static IOverlayTopPlus OverlayTopPlus;
        }

        #endregion

        #region Single Services.

        #endregion

#if UNITY_EDITOR
        public static class Editor
        {
            public static Action<IDestination> SetEditorDestination;
            public static ScriptableObject SavedValueRegistry;
        }

#endif
    }
}
