using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxCamera : MonoBehaviour
{


    public Transform trueCamera;


    private void Awake()
    {
        if(trueCamera == null) trueCamera = Camera.main.transform;
    }

    private void LateUpdate() => transform.rotation = trueCamera.transform.rotation;
}
