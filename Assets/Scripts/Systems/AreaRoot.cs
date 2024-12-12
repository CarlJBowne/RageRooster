using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaRoot : MonoBehaviour
{
    public AreaTransition[] transitions;
    public Transform defaultPlayerSpawn;

    [HideInInspector] public new string name;

    public static implicit operator string(AreaRoot A) => A.name ?? A.gameObject.scene.name;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Boot()
    {
        var attempt = FindFirstObjectByType<AreaRoot>(FindObjectsInactive.Include);
        if (attempt == null || Gameplay.active) return;
        Gameplay.areaToOpen = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(Gameplay.GAMEPLAY_SCENE_NAME);
        return;
    }

    private void Awake()
    {
        name = gameObject.scene.name;

        if(transitions.Length == 0) transitions = gameObject.GetComponentsInChildren<AreaTransition>();

        AreaManager.Get().LoadArea(this);

    }

    public void Update_()
    {

    }


}