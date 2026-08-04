using System;
using UnityEngine;

namespace RageRooster.Core
{
    public interface IPlayer
    {
        public static IPlayer Self => Services.Player;
        public static bool Present => Self != null;

        Transform Transform { get; }
        Vector3 Position { get; }
        Vector3 Center { get; }

        // Currency
        int CurrencyCurrent { get; }
        event Action OnUpdateCurrency;

        void InstantMove(Vector3 position, float? yRot = null);

        bool Owns(Component C);

        public IPlayerStats Stats { get; set; }
    }

    public interface IPlayerStats
    {
        
    }
}