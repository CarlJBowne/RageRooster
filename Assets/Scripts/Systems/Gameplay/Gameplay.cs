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
    public float minLoadedTime;

    
    public const string GAMEPLAY_SCENE_NAME = "GameplayScene";

    public static GameObject Player => I.player;
    public static Transform CameraTransform => I.cameraTransform;
    public static CinemachineVirtualCamera VirtualCam => I.virtualCam;
    public static PauseMenu PauseMenu => I.pauseMenu;
    public static UIHUDSystem UI => I.uI;
    public static ZoneManager ZoneManager => I.zoneManager;

}
