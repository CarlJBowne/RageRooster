using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RageRooster.Systems.SaveSystem
{
    /// <summary>
    /// A container for the active data of all player upgrades.
    /// </summary>
    [System.Serializable]
    public class Upgrades : ICloneable<Upgrades>
    {
        /// <summary> The ability to throw a grabbable object downwards while in midair, launching the player upwards. </summary>
        public bool dropLaunch;
        /// <summary> The ability to kick off flat walls mid air to gain extra height and reach new areas. </summary>
        public bool wallJump;
        /// <summary> The ability to parry in mid-air and be launched by volcanic vents high into the air. </summary>
        public bool hellcopter;
        /// <summary> The ability to charge with fury, breaking through certain obstacles and enemies. </summary>
        public bool ragingCharge;
        /// <summary> The ability to glide through the air slowly. </summary>
        public bool glide;
        /// <summary> The ability to Jump a second time in mid air. </summary>
        public bool doubleJump;
        /// <summary> The ability to throw a lasso at grabbable objects to grab them from a distance. </summary>
        public bool lasso;
        /// <summary> A debug-privilege upgrade that makes the player invincible. </summary>
        [JsonIgnore] public bool d_invincibility;
        /// <summary> A debug-privilege upgrade that makes the player go infinitely upwards as long as the jump button is held. </summary>
        [JsonIgnore] public bool d_moonJump;

        /// <summary>
        /// A convenient accessor for the currently active upgrades of the player.
        /// </summary>
        public static Upgrades Active => SaveData.Current.playerStats.upgrades;

        /// <returns>A clone of the default upgrades as defined in the <see cref="SavedValueRegistry"/>.</returns>
        public static Upgrades Default => SavedValueRegistry.Upgrades.Clone();

        /// <returns>A new instance of <see cref="Upgrades"/> with all upgrades active, including debug-privilege upgrades</returns>
        public static Upgrades Debug() => new()
        {
            dropLaunch = true,
            wallJump = true,
            hellcopter = true,
            ragingCharge = true,
            glide = true,
            doubleJump = true,
            lasso = true,
            d_invincibility = true,
            d_moonJump = true
        };
        public Upgrades Clone(Upgrades target = null)
        {
            target ??= new Upgrades();
            target.dropLaunch = dropLaunch;
            target.wallJump = wallJump;
            target.hellcopter = hellcopter;
            target.ragingCharge = ragingCharge;
            target.d_invincibility = d_invincibility;
            target.d_moonJump = d_moonJump;
            return target;
        }

        public enum Upgrade
        {
            DropLaunch,
            WallJump,
            Hellcopter,
            RagingCharge,
            Glide,
            DoubleJump,
            Lasso,
        }
        public bool HasUpgrade(Upgrade upgrade)
        {
            return upgrade switch
            {
                Upgrade.DropLaunch => dropLaunch,
                Upgrade.WallJump => wallJump,
                Upgrade.Hellcopter => hellcopter,
                Upgrade.RagingCharge => ragingCharge,
                Upgrade.Glide => glide,
                Upgrade.DoubleJump => doubleJump,
                Upgrade.Lasso => lasso,
                _ => false,
            };
        }
        public bool SetUpgrade(Upgrade upgrade, bool value)
        {
            if (upgrade == Upgrade.DropLaunch) dropLaunch = value;
            if (upgrade == Upgrade.WallJump) wallJump = value;
            if (upgrade == Upgrade.Hellcopter) hellcopter = value;
            if (upgrade == Upgrade.RagingCharge) ragingCharge = value;
            if (upgrade == Upgrade.Glide) glide = value;
            if (upgrade == Upgrade.DoubleJump) doubleJump = value;
            if (upgrade == Upgrade.Lasso) lasso = value;

            return false;
        }
    }
}
