using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RageRooster.Core;
using RageRooster.World;
using SLS.SaveData;
using Utilities.JSON;

namespace RageRooster.Player
{
    /// <summary>
    /// A container for the active data of all player upgrades.
    /// </summary>
    [System.Serializable]
    public class PlayerStats : Saveable<PlayerStats>, IPlayerStats
    {
        public int maxHealth = 3;
        public int maxAmmo = 0;
        public Destination location;

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

        public override void Clone(PlayerStats source)
        {
            maxHealth = source.maxHealth;
            maxAmmo = source.maxAmmo;
            dropLaunch = source.dropLaunch;
            wallJump = source.wallJump;
            hellcopter = source.hellcopter;
            ragingCharge = source.ragingCharge;
            glide = source.glide;
            doubleJump = source.doubleJump;
            lasso = source.lasso;
            location = source.location;
        }




        /// <returns>A new instance of <see cref="PlayerStats"/> with all upgrades active, including debug-privilege upgrades</returns>
        public static void ActivateDebug()
        {
            Current.dropLaunch = true;
            Current.wallJump = true;
            Current.hellcopter = true;
            Current.ragingCharge = true;
            Current.glide = true;
            Current.doubleJump = true;
            Current.lasso = true;
            Current.d_invincibility = true;
            Current.d_moonJump = true;
            Current.maxHealth = 10;
            Current.maxAmmo = 40;
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
