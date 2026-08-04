namespace RageRooster.Core
{
    /// <summary>
    /// An enum representing states of activity for the Player. 
    /// </summary>
    public enum ActivityStates
    {
        /// <summary> The Player has not been loaded in as Gameplay is not active. </summary>
        Null = -1,
        /// <summary> The Player is active and controlled by the player. </summary>
        Active = 0,
        /// <summary> The Player is paused in place, still visible, but not moving. </summary>
        Paused = 1,
        /// <summary> The player is in the dying animation. </summary>
        Dying = 2,
        /// <summary> The player is outside of the visibly active scene and thus unrendered.</summary>
        Invisible = 3,
        /// <summary> The game is in a cutscene state and all active logic on the Player has been paused. </summary>
        Cutscene = 4,
        /// <summary> 
        /// The game is currently in a Minigame state where the player's default behavior is not present. 
        /// </summary>
        Minigame = 5,
    }
}
