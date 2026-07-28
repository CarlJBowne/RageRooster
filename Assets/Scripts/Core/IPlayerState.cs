using System;
using UnityEngine;

namespace RageRooster.Core
{
    public interface IPlayerState
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        Vector3 Center { get; }
        bool Exists { get; }

        // Health
        int HealthCurrent { get; }
        int HealthMax { get; }
        event Action OnUpdateHealth;
        event Action OnUpdateMaxHealth;

        // Ammo
        int AmmoCurrent { get; }
        int AmmoMax { get; }
        event Action OnUpdateAmmo;
        event Action OnUpdateMaxAmmo;

        // Currency
        int CurrencyCurrent { get; }
        event Action OnUpdateCurrency;

        void InstantMove(Vector3 position, float? yRot = null);
    }
}