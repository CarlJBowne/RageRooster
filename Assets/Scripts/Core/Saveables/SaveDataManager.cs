using System;
using System.Collections.Generic;
using System.Text;
using SLS.Singletons;

namespace RageRooster.Core.Save
{
    /// <summary>
    /// Most functionality is defined in the SaveDataRegistry class and callable from <see cref="SaveData"/>
    /// </summary>
    public abstract class SaveDataManager : GlobalAsset<SaveDataManager>
    {
        public abstract void SaveToFile();
        public abstract void RevertToFile();
        public abstract void SaveToDeathData();
        public abstract void RevertToDeathData();
        public abstract void InitializeFile(int fileNo);
        public abstract void ExitFile();

        /// <summary>
        /// The currently Active <see cref="SaveData"/> functioning in the game.
        /// </summary>
        public static SaveData ActiveData { get; protected set; }
        /// <summary>
        /// Stands for Defaults or Definitions depending on your point of view. <br/>
        /// Contains all the default values as well as the definition of what they are called.
        /// </summary>
        public static SaveData DefsData { get; protected set; }
        /// <summary> The Save Data used to reload data after the player experiences a death. </summary>
        /// <remarks> See <see cref="RevertToDeathData"/></remarks>
        public static SaveData DeathReloadData { get; protected set; }

#if UNITY_EDITOR
        new public static void SetDirty() => UnityEditor.EditorUtility.SetDirty(Get);
#endif
    }
}
