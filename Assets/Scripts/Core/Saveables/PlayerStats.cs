using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RageRooster.Core;
using RageRooster.SaveSystem;
using RageRooster.World;
using SLS.GeneralUtilities;
using SLS.SaveData;
using Utilities.JSON;

namespace RageRooster.Player
{
    /// <summary>
    /// A container for the active data of all player upgrades.
    /// </summary>
    [System.Serializable]
    public class PlayerStats : Saveable<PlayerStats>
    {
        private int maxHealth = 3;
        public int MaxHealth
        {
            get => maxHealth;
            set
            {
                maxHealth = value;
                if(this == Active) OnMaxHealthChanged?.Invoke(value);
            }
        }
        public static event Action<int> OnMaxHealthChanged;

        private int maxAmmo = 3;
        public int MaxAmmo
        {
            get => maxAmmo;
            set
            {
                maxAmmo = value;
                if (this == Active) OnMaxAmmoChanged?.Invoke(value);
            }
        }
        public static event Action<int> OnMaxAmmoChanged;
        public IDestination location;

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
            MaxHealth = source.maxHealth;
            MaxAmmo = source.maxAmmo;
            dropLaunch = source.dropLaunch;
            wallJump = source.wallJump;
            hellcopter = source.hellcopter;
            ragingCharge = source.ragingCharge;
            glide = source.glide;
            doubleJump = source.doubleJump;
            lasso = source.lasso;
            location = source.location;
        }

        public override void Establish(string context)
        {
            if (context == SaveData.EstablishmentContexts.Active) Active = this;
            else if (context == SaveData.EstablishmentContexts.Default) Default = this;
        }


        /// <returns>A new instance of <see cref="PlayerStats"/> with all upgrades active, including debug-privilege upgrades</returns>
        public static void ActivateDebug()
        {
            Active.dropLaunch = true;
            Active.wallJump = true;
            Active.hellcopter = true;
            Active.ragingCharge = true;
            Active.glide = true;
            Active.doubleJump = true;
            Active.lasso = true;
            Active.d_invincibility = true;
            Active.d_moonJump = true;
            Active.maxHealth = 10;
            Active.maxAmmo = 40;
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
