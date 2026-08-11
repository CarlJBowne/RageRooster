using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RageRooster.Core
{
    public interface IGameplay
    {
        public static IGameplay Self => RageRooster.Services.Gameplay;
        public static bool Present => Self != null;

        public List<MonoBehaviour> bobAndTurnList { get; }

        public bool Active { get; }

        public void Death();
        public void Respawn();

        public event Action onFinalAwake;
    }
}
