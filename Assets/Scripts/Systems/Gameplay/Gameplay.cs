using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using EditorAttributes;


#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-100)]
public class Gameplay : Singleton<Gameplay>
{

    public GameObject player;
    public PlayerStateMachine playerStateMachine;
    public Transform cameraTransform;
    public CinemachineVirtualCamera virtualCam;
    public PauseMenu pauseMenu;
    public UIHUDSystem uI;
    public ZoneManager zoneManager;
    public GlobalState globalState;
    public SettingsMenu settingsMenu;

    public static string spawnSceneName = null;
    public static int spawnPointID = -1;


    public const string GAMEPLAY_SCENE_NAME = "GameplayScene";

    public static GameObject Player => I.player;
    public static PlayerStateMachine PlayerStateMachine => I.playerStateMachine;
    public static Transform CameraTransform => I.cameraTransform;
    public static CinemachineVirtualCamera VirtualCam => I.virtualCam;
    public static PauseMenu PauseMenu => I.pauseMenu;
    public static UIHUDSystem UI => I.uI;
    public static ZoneManager ZoneManager => I.zoneManager;
    public static GlobalState GlobalState => I.globalState;

    protected static System.Action PostMaLoad;
    public static StudioEventEmitter musicEmitter;

    /// <summary>
    /// Begins the main menu by loading the gameplay scene and setting the active save file.
    /// </summary>
    /// <param name="fileNo">The File Number. Intended to be set somewhere in the Main Menu.</param>
    public static void BeginMainMenu(int fileNo)
    {
        if (Gameplay.Active) return;
        GlobalState.activeSaveFile = fileNo;
        SceneManager.LoadScene(GAMEPLAY_SCENE_NAME);
    }

    /// <summary>
    /// Begins a new scene by loading the specified scene.
    /// </summary>
    /// <param name="sceneToLoad">The name of the Scene to Load.</param>
    public static void BeginScene(string sceneToLoad)
    {
        if (Gameplay.Active) return;
        PostMaLoad += () =>
        {
            if (spawnSceneName != sceneToLoad)
            {
                spawnSceneName = sceneToLoad;
                spawnPointID = -1;
            }
        };
        SceneManager.LoadScene(GAMEPLAY_SCENE_NAME);
    }

    /// <summary>
    /// Begins a scene from a save point by loading the specified scene and spawn point.
    /// </summary>
    /// <param name="sceneToLoad">The name of the Scene to Load.</param>
    /// <param name="spawnID">The Intended Spawn Point ID.</param>
    public static void BeginSavePoint(string sceneToLoad, int spawnID)
    {
        if (Gameplay.Active) return;
        PostMaLoad += () =>
        {
            spawnSceneName = sceneToLoad;
            spawnPointID = spawnID;
        };
        SceneManager.LoadScene(GAMEPLAY_SCENE_NAME);
    }

    /// <summary>
    /// Called when the Gameplay singleton is awakened. Loads the global state and initializes the zone manager.
    /// </summary>
    protected override void OnAwake()
    {
        musicEmitter = GetComponent<StudioEventEmitter>();
        GlobalState.Load();

        PostMaLoad?.Invoke();

        zoneManager.Awake();

        SceneManager.LoadScene(spawnSceneName ?? ZoneManager.Get().defaultAreaScene, LoadSceneMode.Additive);

        ZoneManager.OnFirstLoad += OnFirstLoad;

        Input.UI.PauseGame.performed += c =>
        {
            Menu.Manager.Escape();
        };

        
    }

    /// <summary>
    /// Called on the first load of the zone manager. Moves the player to the spawn point and activates the player.
    /// </summary>
    private void OnFirstLoad()
    {
        SavePoint spawn = ZoneManager.CurrentZone.GetSpawn(spawnPointID);
        Player.GetComponent<PlayerStateMachine>().InstantMove(spawn);
        Player.gameObject.SetActive(true);
    }

    /// <summary>
    /// Spawns the player by starting a coroutine.
    /// </summary>
    public static void SpawnPlayer() => new CoroutinePlus(Get().SpawnPlayer_CR(), Get());

    /// <summary>
    /// Resets the game to the saved state by starting a coroutine.
    /// </summary>
    public void ResetToSaved() => new CoroutinePlus(Get().ResetToSaved_CR(), Get());

    /// <summary>
    /// Coroutine for spawning the player. Loads the spawn scene if not already loaded and moves the player to the spawn point.
    /// </summary>
    private IEnumerator SpawnPlayer_CR()
    {
        if (!ZoneManager.ZoneIsReady(spawnSceneName)) SceneManager.LoadScene(spawnSceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => ZoneManager.ZoneIsReady(spawnSceneName));

        ZoneManager.DoTransition(spawnSceneName);
        Player.GetComponent<PlayerStateMachine>().InstantMove(ZoneManager.CurrentZone.GetSpawn(spawnPointID));
    }

    /// <summary>
    /// Coroutine for resetting the game to the saved state. Unloads all zones, loads the global state, and moves the player to the spawn point.
    /// </summary>
    private IEnumerator ResetToSaved_CR()
    {
        Player.SetActive(false);
        yield return ZoneManager.Get().UnloadAll();

        GlobalState.Load();
        SceneManager.LoadScene(spawnSceneName ?? ZoneManager.Get().defaultAreaScene, LoadSceneMode.Additive);

        yield return new WaitUntil(() => ZoneManager.ZoneIsReady(spawnSceneName));
        ZoneManager.DoTransition(spawnSceneName);
        Player.GetComponent<PlayerStateMachine>().InstantMove(ZoneManager.CurrentZone.GetSpawn(spawnPointID));
    }


    public static void DESTROY(bool areYouSure = false)
    {
        if (!areYouSure)
        {
            #if UNITY_EDITOR
            Debug.Log("Someone is trying to Destroy the gameplay without realizing the gravity of that situation.");
            #endif
            return;
        }
        DestroyS();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Gameplay), true)]
public class GameplayEditor : Editor
{
    Gameplay This;

    void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        GUILayout.Label(Gameplay.spawnSceneName);
        GUILayout.Label(Gameplay.spawnPointID.ToString());

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

