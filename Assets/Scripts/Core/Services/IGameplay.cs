using System;
using System.Collections.Generic;
using System.Text;

namespace RageRooster.Core
{
    public interface IGameplay
    {
        public static IGameplay Self => RageRooster.Services.Gameplay;
        public static bool Present => Self != null;

        public bool Active { get; }
    }
}
