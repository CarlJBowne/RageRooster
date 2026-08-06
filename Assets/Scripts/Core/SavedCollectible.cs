using System.Collections.Generic;
using SLS.SaveData;

namespace RageRooster.SaveSystem
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
        public int total = 0;
        /// <summary>
        /// A list of individual collectibles and whether they are collected or not.<br/>
        /// </summary>
        public List<bool> isCollected;

        public override void Clone(SavedCollectible source)
        {
            if (this.GetType() != source.GetType()) return;
            total = source.total;
            isCollected ??= new List<bool>(source.isCollected);
        }

        public static SavedCollectible Wishbones;
        public static SavedCollectible PowerEggs;
        public static SavedCollectible Hens;
    }
}
