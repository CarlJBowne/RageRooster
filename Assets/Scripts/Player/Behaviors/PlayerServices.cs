using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageRooster.Player
{
    public static class Services
    {
        public static PlayerRoot Self => Player;
        public static PlayerRoot Player;

        public static bool Active() =>
            Player != null
            && RageRooster.Services.Player != null
            && Player.GameObject != null
            && Player.gameObject != null;
    }
}