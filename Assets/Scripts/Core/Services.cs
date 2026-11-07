using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using RageRooster.Core;
using RageRooster.Core.World;
using UnityEngine;

namespace RageRooster
{
    /// <summary>
    /// Static Services available across the project. 
    /// <br/> Add "using static RageRooster.Services;" to gain instant access to all services.
    /// </summary>
    public static class Services
    {
        #region Main Services
        public static IPlayer Player => IPlayer.Get;
        public static IMusicService Music => IMusicService.Self;
        #endregion

        #region Static Services

        public static class UI
        {
            public static IOverlayTopPlus OverlayTopPlus;
        }

        #endregion

#if UNITY_EDITOR
        public static class Editor
        {
            public static Action<Destination> SetEditorDestination;
            public static ScriptableObject SavedValueRegistry;
        }

#endif
    }
}
