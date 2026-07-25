using System;
using System.Collections;
using System.Collections.Generic;
using RageRooster.RoomSystem;
using RageRooster.Systems;
using RageRooster.Systems.SaveSystem;
using SLS.GameStateMachine;
using SLS.MenuCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;
using SLS.ObjectUtilities;

public class Gameplay : GameStateSingle<Gameplay>
{
    public override bool Additive => false;
    public static GameObject[] rootObjects;

    protected override void TransitionLogic(Action SetCurrent, Action PostAction)
    {
        E().Begin();
        IEnumerator E()
        {
            SetCurrent();
            SceneManager.LoadScene(Scene, LoadSceneMode.Single);
            var s = SceneManager.GetSceneByName(Scene);
            yield return null;
            rootObjects = s.GetRootGameObjects(); 
            yield return null;

            for (int i = 0; i < rootObjects.Length; i++) DontDestroyOnLoad(rootObjects[i]);
            //rootObjects[1].GetComponent<Player>().Awake();
            //rootObjects[2].GetComponent<Cameras>().Awake();

            PostAction();

            yield return null;

            GlobalPool.poolParent = rootObjects[0].transform.Find("PooledObjects");
            GlobalPool.Get.Initialize();
            Overlay.OverALL.Alpha = 1;
            Overlay.UnderHUD.ResetState();
            Overlay.BetweenUI.ResetState();

            yield return WaitFor.Until(Initialized);

            static bool Initialized() => Active
                && Player.Active
                && RoomManager.Active;

            RoomManager.ResetTransitionData(false);
            EntitySpawn.PlayerPosition = Player.Transform;

            RoomManager.TransitionStyle = new()
            {
                forceFullTransition = true,
                FadeOutRoutine = null,
                FadeInRoutine = Overlay.OverALL.FadeAlpha(0, 0.5f),
                PreFadeInAction = () =>
                {
                    Overlay.UnderHUD.ResetState();
                    Overlay.BetweenUI.ResetState();
                    OverlayTopPlus.Get.ResetState();
                    UpdateGameTime();
                    Input.Pause.performed += c => { Menu.Escape(); };
                    Menu.EscapeCallbackMenuless += PauseMenu.Get.Open;
                },
            };
            yield return RoomManager.Transition();
            onFinalAwake?.Invoke();
        }
    }



    //[RuntimeInitializeOnLoadMethod]
    //static void InitServices()
    //{
    //    Services.Gameplay.Active = new(() => Active);
    //    Services.Gameplay.ReloadSave = ReloadSave;
    //    Services.Gameplay.Respawn = Respawn;
    //    Services.Gameplay.EndGame = EndGame;
    //}

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


    /// <summary>
    /// Begins The Gameplay Phase using the specified Save File on Disk.
    /// </summary>
    /// <param name="fileNo"></param>
    public static void BeginSaveFile(int fileNo)
    {
        if (Active) return;

        Enum().Begin(Overlay.OverALL);
        IEnumerator Enum()
        {

            yield return Overlay.OverALL.FadeAlpha(1);

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            InitializeSaves(fileNo);
            RoomManager.destination = SaveData.Current.location;

            Menu.CloseAllMenus();

            Get.Enter();

            yield return WaitFor.Until(() => Get.isActive);
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

        Get.Enter();
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

    }


    

}
