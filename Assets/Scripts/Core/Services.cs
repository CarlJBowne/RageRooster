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
        public static IPlayer Player { get; internal set; }
        public static IGameplay Gameplay { get; internal set; }
        public static IMusicService Music { get; internal set; }
        public static IHUDService HUD { get; internal set; }
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
        }

        #endregion

        #region Single Services.


        #endregion

        /// <summary>
        /// Registration Center for Static Services.
        /// <br/> Be sure to Deregister when services stop existing by calling the Register function with null input.
        /// </summary>
        public static class Register
        {
            public static void Player(IPlayer input) => Services.Player = input;
            public static void Gameplay(IGameplay input) => Services.Gameplay = input;
            public static void Music(IMusicService input) => Services.Music = input;
            public static void HUD(IHUDService input) => Services.HUD = input;
        }
    }
}
