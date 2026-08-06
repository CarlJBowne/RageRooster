using System;
using System.Collections.Generic;
using System.Text;
using RageRooster.Core;
using RageRooster.World;

namespace RageRooster
{
    /// <summary>
    /// Static Services available across the project. 
    /// <br/> Add "using static RageRooster.Services;" to gain instant access to all services.
    /// </summary>
    public class Services
    {
        #region Main Services
        public static IPlayer Player;
        public static IGameplay Gameplay;
        public static IMusicService Music;
        public static IHUDService HUD;
        public static IOverlayTopPlus OverlayTopPlus;
        #endregion

        #region Static Services

        public static class SaveSystem
        {
            public static GSService<IDestination> CurrentDestination;
            public static GSService<IDestination> DeathDestination;

            public static Action SaveToDeathData;
            public static Action RevertToDeathData;
            public static Action SaveToSaveFile;
            public static Action RevertToSaveFile;

            public static Action<IDestination> GetEditorDestination;

            public static ISaveData Active;
        }

        public static class UI
        {
            public static bool canPause;
            public static Action<bool> SetPause;
            public static Action<bool> LoadingPopup;
        }

        #endregion

        #region Single Services.


        #endregion
    }
}
