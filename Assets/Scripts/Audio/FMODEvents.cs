using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("Player SFX")]
    [field: SerializeField] public EventReference playerJump { get; private set; }
    [field: SerializeField] public EventReference playerLand { get; private set; }
    [field: SerializeField] public EventReference playerDeath { get; private set; }
    
    [field: Header("Music")]
    [field: SerializeField] public EventReference mainMenuMusic { get; private set; }
    [field: SerializeField] public EventReference forestMusic { get; private set; }
    [field: SerializeField] public EventReference ranchMusic { get; private set; }
    
    [field: Header("SFX")]
    [field: SerializeField] public EventReference laserShot { get; private set; }

    [field: Header("UI")]
    [field: SerializeField] public EventReference buttonPressed { get; private set; }

    [field: Header("Ambience")]
    [field: SerializeField] public EventReference forestAmbience { get; private set; }
    [field: SerializeField] public EventReference ranchAmbience { get; private set; }

    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        // Ensure only one instance of FMODEvents exists and initialize it
        if (instance != null)
        {
            Debug.LogError("Found more than one FMOD Event in the scene.");
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("FMODEvents Awake");

        if (forestAmbience.Guid == System.Guid.Empty && ranchAmbience.Guid == System.Guid.Empty)
        {
            Debug.LogWarning("No ambience events are assigned in the FMODEvents component.");
        }
    }

    public bool HasAmbience()
    {
        // Check if any ambience events are assigned
        return forestAmbience.Guid != System.Guid.Empty || ranchAmbience.Guid != System.Guid.Empty;
    }

    public EventReference GetAmbience()
    {
        // Get the assigned ambience event
        if (forestAmbience.Guid != System.Guid.Empty)
        {
            return forestAmbience;
        }
        else if (ranchAmbience.Guid != System.Guid.Empty)
        {
            return ranchAmbience;
        }
        else
        {
            return new EventReference();
        }
    }

    public bool HasMusic()
    {
        // Check if any music events are assigned
        return mainMenuMusic.Guid != System.Guid.Empty || forestMusic.Guid != System.Guid.Empty || ranchMusic.Guid != System.Guid.Empty;
    }

    public EventReference GetMusic()
    {
        // Get the assigned music event
        if (mainMenuMusic.Guid != System.Guid.Empty)
        {
            return mainMenuMusic;
        }
        else if (forestMusic.Guid != System.Guid.Empty)
        {
            return forestMusic;
        }
        else if (ranchMusic.Guid != System.Guid.Empty)
        {
            return ranchMusic;
        }
        else
        {
            return new EventReference();
        }
    }
}
