using System;
using System.Runtime.CompilerServices;
using SLS.Singletons;
using UnityEngine;


namespace RageRooster.Core
{
    public interface IPlayer
    {
        public static IPlayer Get => Register.Get;
        public static bool TryGet(out IPlayer res) => Register.TryGet(out res);
        public static bool Active => Register.Active;

        public readonly static SingletonLocked<IPlayer> Register = new ("ANGUS_BUCKEYE_IS_RAD"); 
        public static bool Present => Get != null;

        Transform Transform { get; }
        GameObject GameObject { get; }
        Vector3 Position { get; }
        Vector3 Center { get; }
        CapsuleCollider Collider { get; }

        ActivityStates ActivityState { get; set; }
        IPlayerStateMachine StateMachine { get; }

        void InstantMove(Vector3 position, float? yRot = null);
        void Death();
        void Respawn();
        bool Owns(Component C);

        float DistanceFrom(Vector3 pos);

        event Action OnMovingUpdate;
        event Action OnRespawn;

        public MonoBehaviour CurrentVent { get; set; }


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
}
