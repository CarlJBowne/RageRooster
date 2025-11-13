using System.Collections.Generic;
using UnityEngine;


public class TargetingManager : MonoBehaviour
{

    public static List<Target> ALLTARGETS = new();

    private static Vector3 FocusPosition;
    private static Vector3 FocusDirection;

    public static void SetFocus(Vector3 position, Vector3 direction)
    {
        FocusPosition = position;
        FocusDirection = direction;
    }














}

public class Target : MonoBehaviour
{

}