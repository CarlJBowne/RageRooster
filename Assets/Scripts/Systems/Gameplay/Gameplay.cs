using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using EditorAttributes;
using System.Collections.Generic;



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
    public DontDestroyMeOnLoad overlayPrefab;

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
    public static System.Action PreReloadSave;
    public static bool fullyLoaded;

    /// <summary>
    /// Begins the main menu by loading the gameplay scene and setting the active save file.
    /// </summary>
    /// <param name="fileNo">The File Number. Intended to be set somewhere in the Main Menu.</param>
    public static void BeginMainMenu(int fileNo)
    {
        if (Gameplay.Active) return;

        Overlay.OverMenus.StartCoroutine(Enum()); 
        IEnumerator Enum()
        {
            Overlay.OverMenus.BasicFadeOut();
            yield return WaitFor.SecondsRealtime(1f);

            GlobalState.activeSaveFile = fileNo;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var Load = SceneManager.LoadSceneAsync(GAMEPLAY_SCENE_NAME);

            yield return WaitFor.Until(() => Load.isDone && fullyLoaded);
            yield return WaitFor.SecondsRealtime(0.2f);
            Overlay.OverMenus.BasicFadeIn();
        }
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
        StartCoroutine(Enum());
        IEnumerator Enum()
        {
            musicEmitter = GetComponent<StudioEventEmitter>();



            yield return WaitFor.Until(() => PlayerHealth.Global.playerObject && PlayerRanged.Ammo.playerObject);

            GlobalState.Load();
            PostMaLoad?.Invoke();

            spawnSceneName ??= ZoneManager.Get().defaultAreaScene;

            if (Overlay.ActiveOverlays.Count == 0) Instantiate(overlayPrefab);

            SceneManager.LoadScene(spawnSceneName, LoadSceneMode.Additive);

            ZoneManager.OnFirstLoad += OnFirstLoad;

            Input.Pause.performed += c => { Menu.Manager.Escape(); };
        }
    }

    /// <summary>
    /// Called on the first load of the zone manager. Moves the player to the spawn point and activates the player.
    /// </summary>
    private void OnFirstLoad()
    {
        SavePoint spawn = ZoneManager.CurrentZone.GetSpawn(spawnPointID);
        spawnPointID = spawn.GetID();
        PlayerStateMachine.InstantMove(spawn);
        PlayerHealth.Global.UpdateMax(GlobalState.maxHealth);
        Player.SetActive(true);
        fullyLoaded = true;
    }

    /// <summary>
    /// Spawns the player by starting a coroutine.
    /// </summary>
    public static void RespawnFromMenu()
    {
        new CoroutinePlus(SpawnPlayer_CR(), Get());

        /// <summary>
        /// Coroutine for spawning the player. Loads the spawn scene if not already loaded and moves the player to the spawn point.
        /// </summary>
        static IEnumerator SpawnPlayer_CR()
        {
            Overlay.OverMenus.BasicFadeOut(1f);
            yield return WaitFor.SecondsRealtime(1f);

            if (!ZoneManager.ZoneIsReady(spawnSceneName)) SceneManager.LoadScene(spawnSceneName, LoadSceneMode.Additive);

            yield return new WaitUntil(() => ZoneManager.ZoneIsReady(spawnSceneName));

            ZoneManager.DoTransition(spawnSceneName);
            PlayerStateMachine.InstantMove(ZoneManager.CurrentZone.GetSpawn(spawnPointID));

            PauseMenu.TrueClose();
            Overlay.OverMenus.BasicFadeIn(1f);
        }
    }

    public static IEnumerator RespawnPlayer()
    {
        if (!ZoneManager.ZoneIsReady(spawnSceneName)) SceneManager.LoadScene(spawnSceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => ZoneManager.ZoneIsReady(spawnSceneName));

        ZoneManager.DoTransition(spawnSceneName);
        PlayerStateMachine.InstantMove(ZoneManager.CurrentZone.GetSpawn(spawnPointID));
    }

    /// <summary>
    /// Resets the game to the saved state by starting a coroutine.
    /// </summary>
    public void ResetToLastSave()
    {
        PreReloadSave?.Invoke();
        new CoroutinePlus(ResetToSaved_CR(), Get());

        /// <summary>
        /// Coroutine for resetting the game to the saved state. Unloads all zones, loads the global state, and moves the player to the spawn point.
        /// </summary>
        static IEnumerator ResetToSaved_CR()
        {
            Overlay.OverMenus.BasicFadeOut(1.2f);
            yield return WaitFor.SecondsRealtime(1.2f);

            Player.SetActive(false);
            yield return ZoneManager.Get().UnloadAll();

            GlobalState.Load();
            SceneManager.LoadScene(spawnSceneName ?? ZoneManager.Get().defaultAreaScene, LoadSceneMode.Additive);

            yield return new WaitUntil(() => ZoneManager.ZoneIsReady(spawnSceneName));
            ZoneManager.DoTransition(spawnSceneName);
            PlayerStateMachine.InstantMove(ZoneManager.CurrentZone.GetSpawn(spawnPointID));

            PauseMenu.TrueClose();
            Overlay.OverMenus.BasicFadeIn(1.2f);
        }
    }

    private const float bobSpeed = 1f;
    private const float rotateSpeed = 90f;
    private void FixedUpdate()
    {
        float time = Time.time;
        float bob = Mathf.Sin(time * bobSpeed);
        float rotate = time * rotateSpeed;

        for (int i = 0; i < bobAndTurnList.Count; i++) bobAndTurnList[i].DoUpdate(bob, rotate);
    }
    public static List<BobAndTurn> bobAndTurnList = new(); 





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

