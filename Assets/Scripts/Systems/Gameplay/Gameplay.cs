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

[DefaultExecutionOrder(-100)]
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

    public static SaveFile SaveData;
    public static SaveFile DeathReloadData;


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

            EnemyCullingGroup.Initialize(this);

            yield return RoomManager.TransitionIn();
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
            RoomManager.transitionDestination = SaveData.location;

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
        RoomManager.transitionDestination = EditorState.EditorDestination;

        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }

    public static void InitializeSaves(int fileNo)
    {
        SaveFile.IO.SetFileTarget(fileNo);
        SaveFile.IO.Load();

        SaveFile.IO.file.Clone(SaveData);
        SaveData.Clone(DeathReloadData);
    }

    private static Destination CalculateEditorSpawn()
    {
        Destination target = EditorState.EditorDestination;

        // If target is default, use the save file location
        if (target.IsDefault()) return SaveData.location;

        Destination fileDest = SaveData.location;

        if(target.area == null && target.room != null) target.area = target.room.area;

        // Fill in missing area from save file if needed
        if (target.area == null) target.area = fileDest.area;

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

    [Obsolete("Unfinished", true)]
    public static IEnumerator PitRespawn()
    {
        yield return Overlay.OverGameplay.BasicFadeOutWait(.5f);

        Player.onRespawn?.Invoke();

        Overlay.OverGameplay.BasicFadeIn(.5f);
    }

    [Obsolete("Unfinished", true)]
    public static IEnumerator Death()
    {
        yield return Overlay.OverGameplay.GameOverAnim();
        yield return WaitFor.SecondsRealtime(Player.deathTime);
        yield return Overlay.OverMenus.BasicFadeOutWait(1f);
        Player.Health.Current = Player.Health.Max;

        Overlay.OverGameplay.Reset();

        Overlay.OverMenus.BasicFadeIn(1f);
    }

    [Obsolete("Unfinished", true)]
    public static IEnumerator ReturnToSpawnpoint()
    {
        yield return null;
    }

    [Obsolete("Unfinished", true)]
    public static IEnumerator ReturnToCheckpoint()
    {
        yield return null;
    }

    [Obsolete("Unfinished", true)]
    public static IEnumerator ReloadSave()
    {
        yield return null;
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




    public static void QuitToTitle()
    {

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
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(Gameplay))]
    public class Editor : UnityEditor.Editor
    {
    }
#endif
}
