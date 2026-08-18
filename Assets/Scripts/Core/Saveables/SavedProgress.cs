using System;
using SLS.GeneralUtilities.StatObjects;
using SLS.SaveData;
using UnityEngine;

namespace RageRooster.Core.Save
{
    public class SavedProgress : Saveable<SavedProgress>
    {
        public static SavedProgress Active { get; private set; }
        public void Establish() => Active = this;

        public TimeSpan playTime = TimeSpan.Zero;

        public IntStat Currency;

        public SavedFlagSet.StoryFlags storyFlags;
        public SavedCollectible powerEggs = new();
        public SavedCollectible wishbones = new();
        public SavedCollectible hensRescued = new();

        /// <summary>
        /// The last written time (in seconds) since the game been started that the player interacted with a save point. <br/>
        /// See <see cref="UpdateGameTime"/>
        /// </summary>
        public static double lastSaveInteractionTime;
        /// <summary>
        /// Updates the <see cref="lastSaveInteractionTime"/> to the current time, returning the time (in seconds) since the last update. <br/>
        /// </summary>
        /// <returns></returns>
        public static double UpdateGameTime()
        {
            var previousSaveInteractionTime = lastSaveInteractionTime;
            lastSaveInteractionTime = Time.timeAsDouble;
            return Time.timeAsDouble - previousSaveInteractionTime;
        }

        public override void Clone(SavedProgress source)
        {
            playTime = source.playTime;
            Currency &= source.Currency;
            powerEggs.Clone(source.powerEggs);
            wishbones.Clone(source.wishbones);
            hensRescued.Clone(source.hensRescued);
        }
    }
}
