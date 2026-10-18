using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLS.Singletons;

namespace RageRooster.Player
{
    public static class LocalContext
    {
        internal static readonly Singleton<PlayerRoot> Register = new();
        public static PlayerRoot Self => Register.Get;
        public static PlayerRoot Player => Register.Get;

        public static bool Active() =>
            Self != null
            && RageRooster.Services.Player != null
            && Player.GameObject != null
            && Player.gameObject != null;
    }
}