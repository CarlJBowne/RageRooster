using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("Player SFX")]
    [field: SerializeField] public EventReference playerJump { get; private set; }
    [field: SerializeField] public EventReference playerSecondJump { get; private set; }
    [field: SerializeField] public EventReference playerHover { get; private set; }
    [field: SerializeField] public EventReference playerFootsteps { get; private set; }
    [field: SerializeField] public EventReference playerMeleeSwings { get; private set; }
    [field: SerializeField] public EventReference playerGrab { get; private set; }
    [field: SerializeField] public EventReference playerThrow { get; private set; }
    [field: SerializeField] public EventReference playerButtSlam { get; private set; }
    [field: SerializeField] public EventReference playerAimMode { get; private set; }
    [field: SerializeField] public EventReference playerShootEggs { get; private set; }
    [field: SerializeField] public EventReference playerDamage { get; private set; }
    [field: SerializeField] public EventReference playerIdle { get; private set; }
    [field: SerializeField] public EventReference playerDropLaunch { get; private set; }
    [field: SerializeField] public EventReference playerWallKick { get; private set; }
    [field: SerializeField] public EventReference playerRagingChargeActivate { get; private set; }
    [field: SerializeField] public EventReference playerCharging { get; private set; }
    [field: SerializeField] public EventReference playerHellcopterSpin { get; private set; }
    [field: SerializeField] public EventReference playerLand { get; private set; }
    [field: SerializeField] public EventReference playerDeath { get; private set; }
    [field: SerializeField] public EventReference playerAirDash { get; private set; }
    [field: SerializeField] public EventReference eggShotImpact { get; private set; }
    
    [field: Header("Music")]
    [field: SerializeField] public EventReference titleScreenMusic { get; private set; }
    [field: SerializeField] public EventReference rockyFurrowsHubMusic { get; private set; }
    [field: SerializeField] public EventReference ireGorgeMusic { get; private set; }
    [field: SerializeField] public EventReference seethingDepthsMusic { get; private set; }
    [field: SerializeField] public EventReference rancorPeakMusic { get; private set; }
    [field: SerializeField] public EventReference rockyFurrowsBossFightMusic { get; private set; }
    [field: SerializeField] public EventReference ireGorgeBossMusic { get; private set; }
    [field: SerializeField] public EventReference seethingDepthBossMusic { get; private set; }
    [field: SerializeField] public EventReference finalBossMusic { get; private set; }
    [field: SerializeField] public EventReference creditsMusic { get; private set; }
    [field: SerializeField] public EventReference fightMusic1 { get; private set; }
    [field: SerializeField] public EventReference fightMusic2 { get; private set; }
    [field: SerializeField] public EventReference fightMusic3 { get; private set; }
    
    [field: Header("SFX")]
    [field: SerializeField] public EventReference healthPickup { get; private set; }
    [field: SerializeField] public EventReference bullionPickup { get; private set; }
    [field: SerializeField] public EventReference abilityVictoryFanfareSFX { get; private set; }
    [field: SerializeField] public EventReference henFanfareSFX { get; private set; }
    [field: SerializeField] public EventReference goldenKernelFanfareSFX { get; private set; }
    [field: SerializeField] public EventReference wishboneFanFareSFX { get; private set; }
    [field: SerializeField] public EventReference woodenDestructible { get; private set; }
    [field: SerializeField] public EventReference stoneDestructible { get; private set; }
    [field: SerializeField] public EventReference metalDestructible { get; private set; }


    [field: Header("UI")]
    [field: SerializeField] public EventReference selectionHover { get; private set; }
    [field: SerializeField] public EventReference selectionConfirm { get; private set; }
    [field: SerializeField] public EventReference returnExit { get; private set; }
    [field: SerializeField] public EventReference pauseSettings { get; private set; }
    [field: SerializeField] public EventReference pauseGameStats { get; private set; }

    [field: Header("Ambience")]
    [field: SerializeField] public EventReference ireGorgeAmbience { get; private set; }
    [field: SerializeField] public EventReference rockyFurrowsAmbience { get; private set; }
    [field: SerializeField] public EventReference waterSplash { get; private set; }
    [field: SerializeField] public EventReference bossAmbience { get; private set; }
    [field: SerializeField] public EventReference transitionAmbience { get; private set; }

    [field: Header("Enemy SFX")]
    [field: SerializeField] public EventReference enemyDamage { get; private set; }
    [field: SerializeField] public EventReference enemyMeleeSwings { get; private set; }
    [field: SerializeField] public EventReference enemyRangedShots { get; private set; }
    [field: SerializeField] public EventReference enemyDeath { get; private set; }
    [field: SerializeField] public EventReference enemyJetpackIdle { get; private set; }
    [field: SerializeField] public EventReference enemyJetpackMotion { get; private set; }
    [field: SerializeField] public EventReference enemyGrabbed { get; private set; }
    [field: SerializeField] public EventReference enemyHeld { get; private set; }
    [field: SerializeField] public EventReference enemyThrown { get; private set; }
    [field: SerializeField] public EventReference enemyIdleChatter { get; private set; }
    [field: SerializeField] public EventReference enemyImpact { get; private set; }

    // make a header for farm animal sfx

    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        // Ensure only one instance of FMODEvents exists and initialize it
        if (instance != null)
        {
            ////Debug.LogError("Found more than one FMOD Event in the scene.");
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        ////Debug.Log("FMODEvents Awake");

        if (ireGorgeAmbience.Guid == System.Guid.Empty && rockyFurrowsAmbience.Guid == System.Guid.Empty)
        {
            ////Debug.LogWarning("No ambience events are assigned in the FMODEvents component.");
        }
    }

    public bool HasAmbience()
    {
        // Check if any ambience events are assigned
        return ireGorgeAmbience.Guid != System.Guid.Empty || rockyFurrowsAmbience.Guid != System.Guid.Empty;
    }

    public EventReference GetAmbience()
    {
        // Get the assigned ambience event
        if (ireGorgeAmbience.Guid != System.Guid.Empty)
        {
            return ireGorgeAmbience;
        }
        else if (rockyFurrowsAmbience.Guid != System.Guid.Empty)
        {
            return rockyFurrowsAmbience;
        }
        else
        {
            return new EventReference();
        }
    }

    public bool HasMusic()
    {
        // Check if any music events are assigned
        return titleScreenMusic.Guid != System.Guid.Empty || ireGorgeMusic.Guid != System.Guid.Empty || rockyFurrowsHubMusic.Guid != System.Guid.Empty;
    }

    public EventReference GetMusic()
    {
        // Get the assigned music event
        if (titleScreenMusic.Guid != System.Guid.Empty)
        {
            return titleScreenMusic;
        }
        else if (ireGorgeMusic.Guid != System.Guid.Empty)
        {
            return ireGorgeMusic;
        }
        else if (rockyFurrowsHubMusic.Guid != System.Guid.Empty)
        {
            return rockyFurrowsHubMusic;
        }
        else
        {
            return new EventReference();
        }
    }
}
