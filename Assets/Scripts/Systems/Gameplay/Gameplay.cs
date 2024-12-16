using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Gameplay : Singleton<Gameplay>
{

    public GameObject player;
    public Transform cameraTransform;
    public CinemachineVirtualCamera virtualCam;
    public PauseMenu pauseMenu;
    public UIHUDSystem uI;
    public ZoneManager zoneManager;
    public string defaultAreaScene;
    public float minLoadedTime;

    public static string areaToOpen = null;
    public const string GAMEPLAY_SCENE_NAME = "GameplayScene";

    public static GameObject Player => I.player;
    public static Transform CameraTransform => I.cameraTransform;
    public static CinemachineVirtualCamera VirtualCam => I.virtualCam;
    public static PauseMenu PauseMenu => I.pauseMenu;
    public static UIHUDSystem UI => I.uI;
    public static ZoneManager ZoneManager => I.zoneManager;


    protected override void OnAwake()
    {
        SceneManager.LoadScene(areaToOpen ?? defaultAreaScene, LoadSceneMode.Additive);
    }
   



}
