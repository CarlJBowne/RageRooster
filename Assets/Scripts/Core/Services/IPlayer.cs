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

        int CurrencyCurrent { get; }
        event Action<int> OnUpdateCurrency;

        void InstantMove(Vector3 position, float? yRot = null);
        void Death();
        void PitFall();
        bool Owns(Component C);

        IPlayerStats Stats { get; }

        event Action OnMovingUpdate;
        event Action OnRespawn;
    }

    public interface IPlayerStats
    {
        int MaxHealth { get; set; }
        int MaxAmmo { get; set; }
    }
}
