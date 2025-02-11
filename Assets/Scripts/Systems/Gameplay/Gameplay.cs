using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Gameplay : Singleton<Gameplay>
{

    public GameObject player;
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
    public static Transform CameraTransform => I.cameraTransform;
    public static CinemachineVirtualCamera VirtualCam => I.virtualCam;
    public static PauseMenu PauseMenu => I.pauseMenu;
    public static UIHUDSystem UI => I.uI;
    public static ZoneManager ZoneManager => I.zoneManager;
    public static GlobalState GlobalState => I.globalState;

    protected static System.Action PostMaLoad;

    // Begins the main menu by loading the gameplay scene and setting the active save file.
    public static void BeginMainMenu(int fileNo)
    {
        if (Gameplay.Active) return;
        GlobalState.activeSaveFile = fileNo;
        SceneManager.LoadScene(GAMEPLAY_SCENE_NAME);
    }

    // Begins a new scene by loading the specified scene.
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

    // Begins a scene from a save point by loading the specified scene and spawn point.
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

    // Called when the Gameplay singleton is awakened. Loads the global state and initializes the zone manager.
    protected override void OnAwake()
    {
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

    // Called on the first load of the zone manager. Moves the player to the spawn point and activates the player.
    private void OnFirstLoad()
    {
        SavePoint spawn = ZoneManager.CurrentZone.GetSpawn(spawnPointID);
        Player.GetComponent<PlayerStateMachine>().InstantMove(spawn);
        Player.gameObject.SetActive(true);
    }

    // Spawns the player by starting a coroutine.
    public static void SpawnPlayer() => new CoroutinePlus(Get().SpawnPlayer_CR(), Get());

    // Resets the game to the saved state by starting a coroutine.
    public void ResetToSaved() => new CoroutinePlus(Get().ResetToSaved_CR(), Get());

    // Coroutine for spawning the player. Loads the spawn scene if not already loaded and moves the player to the spawn point.
    private IEnumerator SpawnPlayer_CR()
    {
        if (!ZoneManager.ZoneIsReady(spawnSceneName)) SceneManager.LoadScene(spawnSceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => ZoneManager.ZoneIsReady(spawnSceneName));

        ZoneManager.DoTransition(spawnSceneName);
        Player.GetComponent<PlayerStateMachine>().InstantMove(ZoneManager.CurrentZone.GetSpawn(spawnPointID));
    }

    // Coroutine for resetting the game to the saved state. Unloads all zones, loads the global state, and moves the player to the spawn point.
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

