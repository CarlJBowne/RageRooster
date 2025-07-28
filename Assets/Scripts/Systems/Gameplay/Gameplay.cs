using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using EditorAttributes;
using System.Collections.Generic;
using SLS.ISingleton;




#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-100)]
public class Gameplay : SingletonMonoBasic<Gameplay>
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

    public static GameObject Player => Get().player;
    public static PlayerStateMachine PlayerStateMachine => Get().playerStateMachine;
    public static Transform CameraTransform => Get().cameraTransform;
    public static CinemachineVirtualCamera VirtualCam => Get().virtualCam;
    public static PauseMenu PauseMenu => Get().pauseMenu;
    public static UIHUDSystem UI => Get().uI;
    public static ZoneManager ZoneManager => Get().zoneManager;
    public static GlobalState GlobalState => Get().globalState;

    protected static System.Action PostMaLoad;
    public static StudioEventEmitter musicEmitter;
    public static System.Action PreReloadSave;
    public static bool fullyLoaded;
    public static System.Action onPlayerRespawn;

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
            
            yield return Overlay.OverMenus.BasicFadeOutWait();

            GlobalState.InitializeSaveFile(fileNo);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Menu.Manager.CloseAllMenus();
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
    protected override void OnInitialize()
    {
        StartCoroutine(Enum());
        IEnumerator Enum()
        {
            musicEmitter = GetComponent<StudioEventEmitter>();

            yield return WaitFor.Until(() => PlayerHealth.Global.playerObject && PlayerRanged.Ammo.playerObject);
            EnemyCullingGroup.Initialize(this);

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

    public static IEnumerator SpawnPlayer()
    {
        spawnSceneName ??= ZoneManager.Get().defaultAreaScene;
        if (!ZoneManager.ZoneIsReady(spawnSceneName)) SceneManager.LoadScene(spawnSceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => ZoneManager.ZoneIsReady(spawnSceneName));

        ZoneManager.DoTransition(spawnSceneName);
        PlayerStateMachine.InstantMove(ZoneManager.CurrentZone.GetSpawn(spawnPointID));
        onPlayerRespawn?.Invoke();
    }

    public static IEnumerator DoReloadSave()
    {
        Player.SetActive(false);
        yield return ZoneManager.UnloadAll();
        GlobalState.Load();
        yield return null;
    }

    protected override void OnDeInitialize() => EnemyCullingGroup.DeInitialize();



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


    public static class EnemyCullingGroup
    {
        static Transform camera;
        public const float enemyCullDistance = 80f;
        static CullingGroup cullingGroup = new();
        static List<CullableEntity> cullableEnemies = new();
        static List<BoundingSphere> enemyBoundingSpheres = new();

        public const float tickTime = 0.1f;
        static CoroutinePlus activeRoutine;
        static WaitForSeconds activeTickDelay = new WaitForSeconds(tickTime);

        public static void Initialize(MonoBehaviour owner)
        {
            cullingGroup.targetCamera = Camera.main;
            camera = Camera.main.transform;
            cullingGroup.onStateChanged += CullingGroupStateUpdate;
            cullingGroup.SetBoundingDistances(new float[] { enemyCullDistance });
            activeRoutine = TickEnum().Begin(owner);
        }
        public static void DeInitialize()
        {
            cullingGroup.Dispose();
            cullableEnemies.Clear();
            enemyBoundingSpheres.Clear();
            activeRoutine?.StopAuto();
        }

        public static IEnumerator TickEnum()
        {
            while (true)
            {
                UpdateCulledEnemies();
                yield return activeTickDelay;
            }
        }

        public static void AddEnemyToCullingGroup(CullableEntity input)
        {
            cullableEnemies.Add(input);
            enemyBoundingSpheres.Add(new(input.transform.position, input.radius));
        }
        public static void RemoveEnemyFromCullingGroup(CullableEntity input)
        {
            if (cullableEnemies.Count < 1) return;
            int ID = cullableEnemies.IndexOf(input);
            cullableEnemies.Remove(input);
            enemyBoundingSpheres.RemoveAt(ID);
        }

        public static void UpdateCulledEnemies()
        {
            cullingGroup.SetDistanceReferencePoint(camera.position);
            for (int i = 0; i < cullableEnemies.Count; i++)
            {
                enemyBoundingSpheres[i] = new BoundingSphere(cullableEnemies[i].transform.position, cullableEnemies[i].radius);

                
            }

            cullingGroup.SetBoundingSpheres(enemyBoundingSpheres.ToArray());
            cullingGroup.SetBoundingSphereCount(cullableEnemies.Count);

            for(int i = 0; i < enemyBoundingSpheres.Count; i++)
            {

                cullableEnemies[i].OnCullEntity(cullingGroup.IsVisible(i));
                
            }
        }

        public static void CullingGroupStateUpdate(CullingGroupEvent @event)
        {
            if (@event.index < 0 || @event.index >= cullableEnemies.Count) return;

            bool currentlyWithin = @event.currentDistance == 0;
            if (currentlyWithin != (@event.previousDistance == 0) || cullableEnemies[@event.index].init)
                cullableEnemies[@event.index].WithinRangeChange(currentlyWithin);
        }


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
        Destroy(Get().gameObject);
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

