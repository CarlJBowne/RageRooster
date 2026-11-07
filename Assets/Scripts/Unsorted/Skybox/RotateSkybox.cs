using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateSkybox : MonoBehaviour
{
    public float speed;

    void Start() => RenderSettings.skybox.SetFloat("_Rotation", 0);
    void OnDestroy() => RenderSettings.skybox.SetFloat("_Rotation", 0);

    void Update() => RenderSettings.skybox.SetFloat("_Rotation", Time.time * speed);
}