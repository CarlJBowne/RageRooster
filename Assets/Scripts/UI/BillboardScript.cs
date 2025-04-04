using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardScript : MonoBehaviour
{
    [Tooltip("Whether to rotate towards the camera on the X axis, otherwise will always point world up.")]
    public bool rotateX;
    [Tooltip("Sprite Renderers have their visible side facing backwards for some reason.")]
    public bool frontIsNegative = true;
    public bool scaleByDistance;
    public Vector2 inputDistance;
    public Vector2 outputScale;

    private new Transform transform;
    private Transform cameraTransform;


    private void Awake()
    {
        transform = GetComponent<Transform>();
        cameraTransform = Camera.main.transform;
    }
    private void Update()
    {
        transform.eulerAngles = new(
            rotateX ? (frontIsNegative ? cameraTransform.eulerAngles.x : -cameraTransform.eulerAngles.x) : transform.eulerAngles.x,
            (frontIsNegative ? cameraTransform.eulerAngles.y : -cameraTransform.eulerAngles.y),
            transform.eulerAngles.z
            );

        if (scaleByDistance)
            transform.localScale = Vector3.one * Mathf.Lerp(outputScale.x, outputScale.y, 
                                                            Mathf.InverseLerp(inputDistance.x, inputDistance.y,
                                                                Vector3.Distance(transform.position, cameraTransform.position)));
    }
}
