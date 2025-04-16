using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(DecalProjector)), DefaultExecutionOrder(50)]
public class DistanceChangingDecal : MonoBehaviour
{
    public float inDistanceLow;
    public float inDistanceHigh;
    public AnimationCurve outputScale;
    public AnimationCurve outputIntensity;
    public LayerMask layerMask;

    Vector2 initialSize;
    float maxRayDistance;

    DecalProjector projector;

    private void Awake()
    {
        if(!TryGetComponent(out projector)) Destroy(this);

        initialSize = projector.size;
        maxRayDistance = projector.size.z;
    }




    private void FixedUpdate()
    {
        bool result = Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, maxRayDistance, layerMask, QueryTriggerInteraction.Ignore);
        projector.enabled = result;
        if (!result) return;
        float scale = hit.distance.Recast(inDistanceHigh, inDistanceLow, outputScale);
        projector.size = new(initialSize.x * scale, initialSize.y * scale, hit.distance + 1f);
        projector.pivot = new(0, 0, projector.size.z / 2);
        projector.fadeFactor = hit.distance.Recast(inDistanceHigh, inDistanceLow, outputIntensity);
    }
}
