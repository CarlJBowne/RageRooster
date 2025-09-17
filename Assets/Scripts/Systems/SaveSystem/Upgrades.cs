using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RageRooster.Systems.SaveSystem
{
    [System.Serializable]
    public class Upgrades
    {
        public bool dropLaunch;
        public bool wallJump;
        public bool hellcopter;
        public bool ragingCharge;
        [JsonIgnore] public bool d_invincibility;
        [JsonIgnore] public bool d_moonJump;

        public static Upgrades Active => SaveFile.Current.playerStats.upgrades;


        public Upgrades Clone() => new()
        {
            dropLaunch = dropLaunch,
            wallJump = wallJump,
            hellcopter = hellcopter,
            ragingCharge = ragingCharge,
            d_invincibility = d_invincibility,
            d_moonJump = d_moonJump
        };

        public static Upgrades Debug() => new()
        {
            dropLaunch = true,
            wallJump = true,
            hellcopter = true,
            ragingCharge = true,
            d_invincibility = true,
            d_moonJump = true
        };

    }
}
