using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ZoneTransition : MonoBehaviour
{
    public GameObject visualProxy;
    public string Scene;
    [SerializeField] float radius = 25f;

    //[HideInInspector] public new Collider collider;
    private Transform playerTransform;
    private float radiusSQR;

    private void Awake()
    {
        if(!ZoneManager.Active || SceneManager.GetSceneByName(Scene) == null)  
        {
            gameObject.SetActive(false);
            return;
        }
        //collider = GetComponent<Collider>();
        playerTransform = Gameplay.Get().player.transform;
        radiusSQR = radius * radius;
        ZoneManager.AddTransition(this);
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif
        ZoneManager.RemoveTransition(this);
    }

    public void SetTraversable(bool value)
    {
        //collider.isTrigger = value;
        if (visualProxy != null) visualProxy.SetActive(!value);
    }

    public bool WithinRange() => playerTransform != null 
        && Vector3.Dot(transform.forward, transform.position - playerTransform.position) > 0 
        && (transform.position - playerTransform.position).sqrMagnitude < radiusSQR;

    private void OnTriggerEnter(Collider other) { if (other.gameObject == Gameplay.Player) ZoneManager.DoTransition(Scene); }

    public static implicit operator string(ZoneTransition A) => A.Scene;
}