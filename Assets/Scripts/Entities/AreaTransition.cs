using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaTransition : MonoBehaviour
{
    public GameObject visualProxy;
    public string Scene;
    [SerializeField] float radius = 25f;

    private Transform playerTransform;
    private float radiusSQR;

    private void Awake()
    {
        playerTransform = Gameplay.Get().player.transform;
        radiusSQR = radius * radius;
    }

    public bool WithinRange() => playerTransform != null 
        && Vector3.Dot(transform.forward, transform.position - playerTransform.position) > 0 
        && (transform.position - playerTransform.position).sqrMagnitude < radiusSQR;

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out PlayerStateMachine PSM)) AreaManager.Get().EnactTransition(Scene);
    }

    public static implicit operator string(AreaTransition A) => A.Scene;
}