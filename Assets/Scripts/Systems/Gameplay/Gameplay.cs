using Cinemachine;
using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.PlayerSettings;


public class Gameplay : Singleton<Gameplay>
{

    public GameObject player;
    public Transform cameraTransform;
    public CinemachineVirtualCamera virtualCam;
    public PauseMenu pauseMenu;
    public UIHUDSystem uI;
    public ZoneManager zoneManager;
    public GlobalState globalState;

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


    public static void BeginMainMenu(int fileNo)
    {
        GlobalState.activeSaveFile = fileNo;
        SceneManager.LoadScene(GAMEPLAY_SCENE_NAME);
    }
    public static void BeginScene(string sceneToLoad)
    {
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
    public static void BeginSavePoint(string sceneToLoad, int spawnID)
    {
        PostMaLoad += () =>
        {
            spawnSceneName = sceneToLoad;
            spawnPointID = spawnID;
        };
        SceneManager.LoadScene(GAMEPLAY_SCENE_NAME);
    }
    protected override void OnAwake()
    {
        
        GlobalState.Load();

        PostMaLoad?.Invoke();

        SceneManager.LoadScene(spawnSceneName ?? ZoneManager.Get().defaultAreaScene, LoadSceneMode.Additive);

        ZoneManager.OnFirstLoad += OnFirstLoad;
    }

    private void OnFirstLoad()
    {
        SavePoint spawn = ZoneManager.CurrentZone.GetSpawn(spawnPointID);
        Player.GetComponent<PlayerStateMachine>().InstantMove(spawn);
        Player.gameObject.SetActive(true);
    }

    public static void SpawnPlayer() => Get().StartCoroutine(Get().SpawnPlayer_CR());
    public void ResetToSaved() => StartCoroutine(Get().ResetToSaved_CR());

    private IEnumerator SpawnPlayer_CR()
    {
        if(!ZoneManager.ZoneIsReady(spawnSceneName)) SceneManager.LoadScene(spawnSceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => ZoneManager.ZoneIsReady(spawnSceneName));

        ZoneManager.DoTransition(spawnSceneName);
        Player.GetComponent<PlayerStateMachine>().InstantMove(ZoneManager.CurrentZone.GetSpawn(spawnPointID));
    }
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

