using System;
using UnityEngine;

namespace RageRooster.Core
{
    public interface IPlayer
    {
        public static IPlayer Self => RageRooster.Services.Player;
        public static bool Present => Self != null;

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

        event Action OnMovingUpdate;
        event Action OnRespawn;

        public MonoBehaviour CurrentVent { get; set; }
    }
}
