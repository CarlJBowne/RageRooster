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
    [field: SerializeField] public EventReference music { get; private set; }

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
        return forestAmbience.Guid != System.Guid.Empty || ranchAmbience.Guid != System.Guid.Empty;
    }

    public EventReference GetAmbience()
    {
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
        return music.Guid != System.Guid.Empty;
    }

    public EventReference GetMusic()
    {
        return music;
    }
}
