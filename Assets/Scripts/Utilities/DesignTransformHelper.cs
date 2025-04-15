using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesignTransformHelper : MonoBehaviour
{
    //[Button]
    public void ApplyScales()
    {
        Vector3 ThisScale = transform.localScale;

    }
    [Button]
    public void CreateAntiScaledChild()
    {
        Vector3 ThisScale = transform.localScale;

        Transform P = transform.parent;
        if (P != null)
        {
            while (P.localScale != Vector3.one)
            {
                ThisScale.Scale(P.localScale);
                P = P.parent;
            }
        }
        

        GameObject child = new GameObject("Child");
        child.transform.parent = transform;
        child.transform.localPosition = Vector3.zero;
        child.transform.localEulerAngles = Vector3.zero;
        child.transform.localScale = new(
            1 * (1 / ThisScale.x),
            1 * (1 / ThisScale.y),
            1 * (1 / ThisScale.z)
            );
    }

























}
