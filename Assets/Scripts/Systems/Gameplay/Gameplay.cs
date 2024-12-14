using UnityEngine;
using UnityEngine.SceneManagement;


public class Gameplay : Singleton<Gameplay>
{

    public GameObject player;
    public Transform cameraTransform;
    public PauseMenu pauseMenu;
    public UIHUDSystem uI;
    public ZoneManager zoneManager;
    public string defaultAreaScene;
    public float minLoadedTime;

    public static string areaToOpen = null;
    public const string GAMEPLAY_SCENE_NAME = "GameplayScene";

    public static GameObject Player => Get().player;
    public static Transform CameraTransform => Get().cameraTransform;
    public static PauseMenu PauseMenu => Get().pauseMenu;
    public static UIHUDSystem UI => Get().uI;
    public static ZoneManager ZoneManager => Get().zoneManager;


    protected override void OnAwake()
    {
        SceneManager.LoadScene(areaToOpen ?? defaultAreaScene, LoadSceneMode.Additive);
    }
   



}
