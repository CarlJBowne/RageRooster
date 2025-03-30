using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement;

public class AudioManager : SingletonAdvanced<AudioManager>
{
    // Set up the AudioManager with specific settings
    static void Data() => SetData(spawnMethod: InitSavedPrefab, dontDestroyOnLoad: true, spawnOnBoot: true);

    // Properties to set the volume for different audio buses
    public float masterVolume { set => masterBus.setVolume(value); }
    public float musicVolume { set => musicBus.setVolume(value); }
    public float SFXVolume { set => sfxBus.setVolume(value); }
    public float ambienceVolume { set => ambienceBus.setVolume(value); }

    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;
    private Bus ambienceBus;

    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;
    private EventInstance ambienceEventInstance;
    public EventInstance musicEventInstance;

    // Called when the script instance is being loaded
    protected override void OnAwake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        eventInstances = new List<EventInstance>();
        eventEmitters = new List<StudioEventEmitter>();

        // Initialize audio buses
        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");
        ambienceBus = RuntimeManager.GetBus("bus:/Ambience");

        // Subscribe to scene load and unload events
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    // Called when the script is started
    private void Start()
    {
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

    // Initialize and start the ambience event
    private void InitializeAmbience(EventReference ambienceEvent)
    {
        ambienceEventInstance = RuntimeManager.CreateInstance(ambienceEvent);
        ambienceEventInstance.start();
    }

    // Initialize and start the music event
    public void InitializeMusic(EventReference musicEventReference)
    {
        musicEventInstance = RuntimeManager.CreateInstance(musicEventReference);
        musicEventInstance.start();
        musicEventInstance.setPaused(false);
    }

    // Set a parameter for the ambience event instance
    public void SetAmbienceParameter(string parameterName, float parameterValue)
    {
        ambienceEventInstance.setParameterByName(parameterName, parameterValue);
    }

    // Play a one-shot sound at the specified position
    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    // Create and return a new event instance
    public EventInstance CreateEventInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    // Create and return a new event emitter
    public StudioEventEmitter CreateEventEmitter(EventReference eventReference, GameObject emitterGameObject)
    {
        StudioEventEmitter emitter = emitterGameObject.GetComponent<StudioEventEmitter>();
        emitter.EventReference = eventReference;
        eventEmitters.Add(emitter);
        return emitter;
    }

    // Clean up all event instances and emitters
    private void CleanUp()
    {
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

    // Called when the AudioManager is destroyed
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        musicEventInstance.setPaused(true);
        CleanUp();
    }

    // Called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //StopSceneMusic();
        //PlaySceneMusic(scene.name);
    }

    // Called when a scene is unloaded
    private void OnSceneUnloaded(Scene scene)
    {
        //StopSceneMusic();
    }

    // Stop and release the current music event instance
    private void StopSceneMusic()
    {
        if (musicEventInstance.isValid())
        {
            musicEventInstance.setPaused(true);
            musicEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicEventInstance.release();
            musicEventInstance.clearHandle();
        }
    }

    // Play the appropriate music for the given scene
    private void PlaySceneMusic(string sceneName)
    {
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
                return;
        }

        if (musicEvent.Guid != System.Guid.Empty)
        {
            InitializeMusic(musicEvent);
        }
        musicEventInstance = RuntimeManager.CreateInstance(musicEvent);
    }
}
