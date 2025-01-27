using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement;

public class AudioManager : SingletonAdvanced<AudioManager>
{
     static void Data() => SetData(spawnMethod: InitFind, dontDestroyOnLoad: true, spawnOnBoot: true);


    [Header("Volume")]
    [Range(0, 1)]
    public float masterVolume = 0.5f;
    [Range(0, 1)]
    public float musicVolume = 0.5f;
    [Range(0, 1)]
    public float SFXVolume = 0.5f;
    [Range(0, 1)]
    public float ambienceVolume = 0.5f;

    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;
    private Bus ambienceBus;

    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;
    private EventInstance ambienceEventInstance;
    public EventInstance musicEventInstance;

    private new void Awake()
    {
        // Initialize the AudioManager and set up buses and event lists
        DontDestroyOnLoad(gameObject);

        eventInstances = new List<EventInstance>();
        eventEmitters = new List<StudioEventEmitter>();

        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");
        ambienceBus = RuntimeManager.GetBus("bus:/Ambience");

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void Start()
    {
        // Initialize ambience and music if available
        ////Debug.Log("AudioManager Start");
        if (FMODEvents.instance != null)
        {
            if (FMODEvents.instance.HasAmbience())
            {
                InitializeAmbience(FMODEvents.instance.GetAmbience());
            }
            if (FMODEvents.instance.HasMusic())
            {
                InitializeMusic(FMODEvents.instance.GetMusic());
            }
        }
    }

    private void Update()
    {
        // Update the volume of each bus
        masterBus.setVolume(masterVolume);
        musicBus.setVolume(musicVolume);
        sfxBus.setVolume(SFXVolume);
        ambienceBus.setVolume(ambienceVolume);
    }
    
    private void InitializeAmbience(EventReference ambienceEvent)
    {
        // Create and start the ambience event instance
        ambienceEventInstance = RuntimeManager.CreateInstance(ambienceEvent);
        ambienceEventInstance.start();
    }

    private void InitializeMusic(EventReference musicEventReference)
    {
        // Create and start the music event instance
        //musicEventInstance = RuntimeManager.CreateInstance(musicEventReference);
        musicEventInstance.setVolume(musicVolume);
        musicEventInstance.start();
        musicEventInstance.setPaused(false);
    }

    public void SetAmbienceParameter(string parameterName, float parameterValue)
    {
        // Set a parameter for the ambience event instance
        ambienceEventInstance.setParameterByName(parameterName, parameterValue);
    }

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        // Play a one-shot sound at the specified position
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public EventInstance CreateEventInstance(EventReference eventReference)
    {
        // Create and return a new event instance
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public StudioEventEmitter CreateEventEmitter(EventReference eventReference, GameObject emitterGameObject)
    {
        // Create and return a new event emitter
        StudioEventEmitter emitter = emitterGameObject.GetComponent<StudioEventEmitter>();
        emitter.EventReference = eventReference;
        eventEmitters.Add(emitter);
        return emitter;
    }

    private void CleanUp()
    {
        // Stop and release all event instances and emitters
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }

        foreach (StudioEventEmitter emitter in eventEmitters)
        {
            emitter.Stop();
        }
    }

    private void OnDestroy()
    {
        // Clean up when the AudioManager is destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        musicEventInstance.setPaused(true);
        CleanUp();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Stop the current scene's music before playing the new scene's music
        StopSceneMusic();
        PlaySceneMusic(scene.name);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // Stop the current scene's music
        StopSceneMusic();
    }

    private void StopSceneMusic()
    {
        // Stop and release the current music event instance
        if (musicEventInstance.isValid())
        {
            ////Debug.Log("Stopping current music event instance.");
            musicEventInstance.setPaused(true);
            musicEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicEventInstance.release();
            musicEventInstance.clearHandle(); // Clear the handle to ensure it's properly reset
        }
        else
        {
            ////Debug.Log("No valid music event instance to stop.");
        }
    }

    private void PlaySceneMusic(string sceneName)
    {
        // Ensure the current music is stopped before playing the new scene's music
        StopSceneMusic();


        EventReference musicEvent = new EventReference();
        switch (sceneName)
        {
            case "MainMenu":
                musicEvent = FMODEvents.instance.titleScreenMusic;
                break;
            case "Forest":
                musicEvent = FMODEvents.instance.ireGorgeMusic;
                break;
            case "FarmHouse":
                musicEvent = FMODEvents.instance.rockyFurrowsHubMusic;
                break;
            default:
                ////Debug.LogWarning($"No music event found for scene: {sceneName}");
                return;
        }

        if (musicEvent.Guid != System.Guid.Empty)
        {
            ////Debug.Log($"Initializing music for scene: {sceneName}");
            InitializeMusic(musicEvent);
        }
        else
        {
            ////Debug.LogWarning($"Music event reference is empty for scene: {sceneName}");
        }
        musicEventInstance = RuntimeManager.CreateInstance(musicEvent);
    }
}
