using System;
using System.Collections.Generic;
using System.Text;

namespace RageRooster.Core
{
    public interface IGameplay
    {
        public static IGameplay Gameplay => Services.Gameplay;

        public bool Active { get; }
    }
}
