using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RageRooster.Systems.SaveSystem
{
    [System.Serializable]
    public class Upgrades : ICloneable<Upgrades>
    {
        public bool dropLaunch;
        public bool wallJump;
        public bool hellcopter;
        public bool ragingCharge;
        [JsonIgnore] public bool d_invincibility;
        [JsonIgnore] public bool d_moonJump;

        public static Upgrades Active => SaveFile.Current.playerStats.upgrades;

        public static Upgrades Default => SavedValueManager.Upgrades.Clone();

        public static Upgrades Debug() => new()
        {
            dropLaunch = true,
            wallJump = true,
            hellcopter = true,
            ragingCharge = true,
            d_invincibility = true,
            d_moonJump = true
        };
        public Upgrades Clone(Upgrades result = null)
        {
            result ??= new Upgrades();
            result.dropLaunch = dropLaunch;
            result.wallJump = wallJump;
            result.hellcopter = hellcopter;
            result.ragingCharge = ragingCharge;
            result.d_invincibility = d_invincibility;
            result.d_moonJump = d_moonJump;
            return result;
        }
    }
}
