using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using EditorAttributes;
using System.Collections.Generic;

using RageRooster.RoomSystem;
using RageRooster.Systems.SaveSystem;
using Utilities.ObjectPooling;
using RageRooster.Systems;
using Utilities;









#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A Global System managing the core gameplay systems and lifecycle. A singleton that persists as long as gameplay is running. <br/>
/// Provides static access to important gameplay-related properties and methods. <br/>
/// To begin gameplay, use methods such as <see cref="BeginSaveFile(int)"/> or <see cref="BeginEditor()"/>.
/// </summary>
[DefaultExecutionOrder(ExecutionOrders.Gameplay)]
public class Gameplay : MonoBehaviour
{
    [InitializeOnLoadMethod]
    static void InitServices()
    {
        Services.Gameplay.GameState = new(() => (Services.Gameplay.GameStates)GameState, value => GameState = (GameStates)value)
        {
            Getter = () => (Services.Gameplay.GameStates)GameState,
            Setter = value => GameState = (GameStates)value
        };
        Services.Gameplay.ReloadSave = ReloadSave;
        Services.Gameplay.Respawn = Respawn;
        Services.Gameplay.EndGame = EndGame;
    }

    public enum GameStates
    {
        Null = -1,
        Active = 0,
        Paused = 1,
        Processing = 2,
    }
    private static GameStates _gameState = GameStates.Null;
    public static GameStates GameState
    {
        get => _gameState;
        set
        {
            if (_gameState == value
                || _gameState is GameStates.Null
                || value is GameStates.Null
                ) return;

            _gameState = value;

            Time.timeScale = value is GameStates.Paused ? 0 : 1;

        }
    }

    /// <summary>
    /// Whether Gameplay is currently active.
    /// <br/> Reads <see cref="GameState"/>, true if not <see cref="GameStates.Null"/>.
    /// </summary>
    public static bool Active => GameState is not GameStates.Null;




    /// <summary>
    /// The Script instance of the Gameplay system. Not truly relevant to much. Null if not active.
    /// <br/> Can be used as the source script for a Coroutine to ensure it runs.
    /// </summary>
    public static Gameplay Instance { get; private set; }
    /// <summary>
    /// The <see cref="UnityEngine.GameObject"/> that this script is attached to. Null if not active."/>
    /// </summary>
    public static GameObject GameObject { get; private set; }

    /// <summary>
    /// A reference to the Scene for this system.
    /// </summary>
    public static SceneReference GAMEPLAY_SCENE = new("GameplayScene");

    /// <summary>
    /// The Emitter that plays gameplay music. 
    /// </summary>
    //public static StudioEventEmitter musicEmitter;

    /// <summary>
    /// Callback event for when a Save is about to be reloaded.
    /// </summary>
    public static System.Action PreReloadSave;
    /// <summary>
    /// A Callback event for when the Gameplay system updates, invoked in <see cref="Update"/>.
    /// </summary>
    public static System.Action onUpdate;
    /// <summary>
    /// A Callback event for when the Gameplay system has finally finished its introduction.
    /// </summary>
    public static System.Action onFinalAwake;
    /// <summary>
    /// A Callbck event for when the Gameplay system is Unloaded.
    /// </summary>
    public static System.Action onDestroy;

    /// <summary>
    /// The last written time (in seconds) since the game been started that the player interacted with a save point. <br/>
    /// See <see cref="UpdateGameTime"/>
    /// </summary>
    public static double lastSaveInteractionTime;

    #region Instance Fields

    [SerializeField] Transform cameraTransform;
    [SerializeField] PauseMenu pauseMenu;
    [SerializeField] UIHUDSystem uI;
    [SerializeField] SettingsMenu settingsMenu;
    [SerializeField] DontDestroyMeOnLoad overlayPrefab;
    [SerializeField] Player inputPlayer;
    [SerializeField] UIHUDSystem inputUI;
    [SerializeField] Cameras inputCams;
    [SerializeField] StudioEventEmitter musicEmitter;
    [SerializeField] StudioEventEmitter musicEmitter2;

    #endregion Instance Fields

    private void Awake()
    {
        if (Active)
        {
            if (Instance != this) Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Instance = this;
        _gameState = GameStates.Active;
        GameObject = gameObject;
        if (Overlay.ActiveOverlays.Count == 0) Instantiate(overlayPrefab);
        DontDestroyOnLoad(gameObject);
        inputPlayer.Awake();
        inputUI.Awake();
        inputCams.Awake();
        GlobalPool.poolParent = transform.Find("PooledObjects");
        GlobalPool.Get.Initialize();
        Overlay.OverMenus.BasicBlackout = 1;
        Overlay.OverGameplay.Reset();
        Overlay.OverHUD.Reset();
        UpdateDelayer.Setup();

        Enum().Begin(this);
        static IEnumerator Enum()
        {
            yield return null;
            yield return WaitFor.Until(Initialized);

            static bool Initialized() => Active
                && Player.Active
                && RoomManager.Active;

            EntitySpawn.PlayerPosition = Player.Transform;

            RoomManager.ResetTransitionData(false);

            RoomManager.TransitionStyle = new()
            {
                forceFullTransition = true,
                FadeOutRoutine = null,
                FadeInRoutine = Overlay.OverMenus.BasicFadeInWait(0.5f),
                PreFadeInAction = () =>
                {
                    UpdateGameTime();
                    Input.Pause.performed += c => { Menu.Manager.Escape(); };
                },
            };
            yield return RoomManager.Transition();
            onFinalAwake?.Invoke();
        }
    }

    private void Update()
    {
        onUpdate?.Invoke();
    }


    /// <summary>
    /// Begins The Gameplay Phase using the specified Save File on Disk.
    /// </summary>
    /// <param name="fileNo"></param>
    public static void BeginSaveFile(int fileNo)
    {
        if (Active) retur
        Enum().Begin(Overlay.OverMenus);
        IEnumerator Enum()
        {

            yield return Overlay.OverMenus.BasicFadeOutWait();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            InitializeSaves(fileNo);
            RoomManager.destination = SaveData.Current.location;

            Menu.Manager.CloseAllMenus();
            var Load = SceneManager.LoadSceneAsync(GAMEPLAY_SCENE);

            yield return WaitFor.Until(() => Load.isDone && Active);
            yield return WaitFor.SecondsRealtime(0.2f);
        }
    }
    /// <summary>
    /// Begins the Gameplay Phase in Editor Mode, using the settings in <see cref="EditorState"/> to determine spawn location. <br/>
    /// </summary>
    public static void BeginEditor()
    {
        if (Active) return;

        InitializeSaves(0);

        if (!EditorState.EditorDestination.IsValid())
            EditorState.EditorDestination = CalculateEditorSpawn();
        if (!EditorState.EditorDestination.IsValid()) EditorState.EditorDestination = Destination.StartingDefault();
        RoomManager.destination = EditorState.EditorDestination;
        EditorState.EditorDestination = Destination.Null;

        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }

    public static void InitializeSaves(int fileNo) => SaveData.InitializeSaves(fileNo);

    private static Destination CalculateEditorSpawn()
    {
        Destination target = EditorState.EditorDestination;

        // If target is default, use the save file location
        if (target.IsNull()) return SaveData.Current.location;

        Destination fileDest = SaveData.Current.location;

        if (EditorState.EditorDestinationArea != null && EditorState.EditorDestinationArea != fileDest.area)
        {
            target.room ??= EditorState.EditorDestinationArea.rooms[0];
            target.spawnID = 0;
            return target;
        }

        // If area matches save file, fill in missing room/spawnID from save file
        if (target.area == fileDest.area)
        {
            if (target.room == null) target.room = fileDest.room;
            if (target.spawnID == -1) target.spawnID = fileDest.spawnID;
        }
        else // If area is different, fill missing room/spawnID with 0th values
        {
            if (target.room == null) target.room = target.area.rooms[0];
            if (target.spawnID == -1) target.spawnID = 0;
        }

        // If room is set but spawnID is missing, fill from save file if area matches, else use 0
        if (target.room != null && target.spawnID == -1)
            target.spawnID = (target.room.area == fileDest.area) ? fileDest.spawnID : 0;

        return target;
    }



    public static void Respawn()
    {
        RoomManager.PostFadeOutAction = () => { Player.onRespawn?.Invoke(); };
        RoomManager.StartTransition(Destination.Current);
    }

    public static void Death()
    {
        SaveData.RevertToDeathData();
        RoomManager.StartTransition(Destination.Current);
    }

    public static void ReloadSave()
    {
        SaveData.RevertToSaveFile();
        RoomManager.StartTransition(Destination.Current);
    }

    /// <summary>
    /// Updates the <see cref="lastSaveInteractionTime"/> to the current time, returning the time (in seconds) since the last update. <br/>
    /// </summary>
    /// <returns></returns>
    public static double UpdateGameTime()
    {
        var previousSaveInteractionTime = lastSaveInteractionTime;
        lastSaveInteractionTime = Time.timeAsDouble;
        return Time.timeAsDouble - previousSaveInteractionTime;
    }





    //protected override void OnDeInitialize() => EnemyCullingGroup.DeInitialize();



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

    /*
    public static class EnemyCullingGroup
    {
        static Transform camera;
        public const float enemyCullDistance = 80f;
        static CullingGroup cullingGroup = new();
        static List<CullableEntity> cullableEnemies = new();
        static List<BoundingSphere> enemyBoundingSpheres = new();

        public const float tickTime = 0.1f;
        static Coroutine activeRoutine;
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


    }*/


    public static void EndGame()
    {
        Music.StopAllMusic();
        Player.StateMachine.HaveDestroyed();
        DESTROY(areYouSure: true);
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
        Destroy(GameObject);
        _gameState = GameStates.Null;

    }

    private void OnDestroy()
    {
        onDestroy?.Invoke();
        //EnemyCullingGroup.DeInitialize();
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(Gameplay))]
    public class Editor : UnityEditor.Editor
    {
    }
#endif
}
