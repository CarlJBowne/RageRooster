using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using EditorAttributes;
using System.Collections.Generic;
using SLS.ISingleton;
using RageRooster.RoomSystem;
using RageRooster.Systems.SaveSystem;






#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(ExecutionOrders.Gameplay)]
public class Gameplay : MonoBehaviour
{

    public static bool Active { get; private set; }
    public static Gameplay Instance { get; private set; }
    public static GameObject GameObject { get; private set; }


    public static string spawnSceneName = null;
    public static int spawnPointID = -1;


    public static SceneReference GAMEPLAY_SCENE = new("GameplayScene");

    public static StudioEventEmitter musicEmitter;
    public static System.Action PreReloadSave;

    public static double lastSaveInteractionTime;

    #region Instance Fields

    [SerializeField] Transform cameraTransform;
    [SerializeField] PauseMenu pauseMenu;
    [SerializeField] UIHUDSystem uI;
    [SerializeField] SettingsMenu settingsMenu;
    [SerializeField] DontDestroyMeOnLoad overlayPrefab;
    [SerializeField] Player inputPlayer;
    [SerializeField] UIHUDSystem inputUI;

    #endregion Instance Fields

    private void Awake()
    {
        if(Active)
        {
            if(Instance != this) Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Instance = this;
        Active = true;
        GameObject = gameObject;
        if (Overlay.ActiveOverlays.Count == 0) Instantiate(overlayPrefab);
        Overlay.OverHUD.SetAlpha(1);
        DontDestroyOnLoad(gameObject);
        inputPlayer.Awake();
        inputUI.Awake();
        GetComponent<Cameras>().Awake();
        musicEmitter = GetComponent<StudioEventEmitter>();
        


        StartCoroutine(Enum());
        IEnumerator Enum()
        {
            yield return null;
            yield return WaitFor.Until(Initialized);

            bool Initialized() => Active
                && Player.Active
                && RoomManager.Active;

            //EnemyCullingGroup.Initialize(this); 

            yield return RoomManager.Transition(true);
            UpdateGameTime();
            Overlay.OverHUD.BasicFadeIn();

            Input.Pause.performed += c => { Menu.Manager.Escape(); };
        }
    }





    public static void BeginSaveFile(int fileNo)
    {
        if (Active) return;

        Enum().Begin(Overlay.OverMenus);
        IEnumerator Enum()
        {

            yield return Overlay.OverMenus.BasicFadeOutWait();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            InitializeSaves(fileNo);
            RoomManager.destination = SaveFile.Current.location;

            Menu.Manager.CloseAllMenus();
            var Load = SceneManager.LoadSceneAsync(GAMEPLAY_SCENE);

            yield return WaitFor.Until(() => Load.isDone && Active);
            yield return WaitFor.SecondsRealtime(0.2f);
            Overlay.OverMenus.BasicFadeIn();
        }
    }
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

    public static void InitializeSaves(int fileNo)
    {
        SaveFile.IO.SetFileTarget(fileNo);
        SaveFile.IO.Load();
        SaveFile.RevertToSaveFile();
    }

    private static Destination CalculateEditorSpawn()
    {
        Destination target = EditorState.EditorDestination;

        // If target is default, use the save file location
        if (target.IsNull()) return SaveFile.Current.location;

        Destination fileDest = SaveFile.Current.location;

        if(EditorState.EditorDestinationArea != null && EditorState.EditorDestinationArea != fileDest.area)
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



    public static IEnumerator Respawn()
    {
        yield return RoomManager.Transition(SaveFile.Current.location);
        Player.onRespawn?.Invoke();
    }

    public static IEnumerator Death()
    {
        SaveFile.RevertToDeathData();
        yield return RoomManager.Transition(SaveFile.Current.location, true);
    }

    public static IEnumerator ReloadSave()
    {
        SaveFile.RevertToSaveFile();
        yield return RoomManager.Transition(SaveFile.Current.location, true);
    }

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


    }*/



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
        Active = false;
    }

    private void OnDestroy()
    {
        //EnemyCullingGroup.DeInitialize();
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(Gameplay))]
    public class Editor : UnityEditor.Editor
    {
    }
#endif
}
