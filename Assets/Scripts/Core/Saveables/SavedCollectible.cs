using System.Collections.Generic;
using SLS.SaveData;

namespace RageRooster.Core.Save
{
    /// <summary>
    /// A Basic Saved Collectible class, tracking the amount and specific collected instances of a collectible. <br/>
    /// Used for <see cref="powerEggs"/>, <see cref="wishbones"/>, and <see cref="hensRescued"/>.
    /// </summary>
    public class SavedCollectible : Saveable<SavedCollectible>
    {
        /// <summary>
        /// The total amount of this collectible that has been collected, only for easy access.
        /// </summary>
        public int collected = 0;
        /// <summary>
        /// A list of individual collectibles and whether they are collected or not.<br/>
        /// </summary>
        public List<bool> isCollected;
        /// <summary>
        /// A list of string IDs for each collectible, used for saving and loading, and editor management.
        /// </summary>
        public List<string> IDs;

        public int total => isCollected.Count;

        public override void Clone(SavedCollectible source)
        {
            if (this.GetType() != source.GetType()) return;
            collected = source.collected;
            isCollected ??= new List<bool>(source.isCollected);
            IDs ??= new List<string>(source.IDs);
        }

        public bool GetValue(string id)
        {
            int index = IDs.IndexOf(id);
            return index != -1 && isCollected[index];
        }
        public bool SetValue(string id, bool value)
        {
            int index = IDs.IndexOf(id);
            if (index == -1) return false;
            isCollected[index] = value;
            return true;
        }

        public float CompletionOf(float percentage) => collected / total * percentage;


        public static SavedCollectible Wishbones;
        public static SavedCollectible PowerEggs;
        public static SavedCollectible Hens;
    }
}
