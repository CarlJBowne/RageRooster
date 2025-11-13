using System.Collections.Generic;
using UnityEngine;

public class TargetingManager : MonoBehaviour
{
    // Static runtime state
    private static List<Target> ALLTARGETS = new();
    private static bool isAimingDownSights = false;
    private static float MaxDistance;
    private static float MaxAngle;
    private static Transform Aim;

    public static TargetingManager Instance { get; private set; }

    // Derived static properties

    #region Instance Fields

    [Tooltip("Determines the weighting between distance from the player and angle from the targeting retical. 1 = All Distance, 0 = All Angle. Allways keep between .1 and .9")]
    [SerializeField] float distanceAngleWeighting;
    public static float DistanceAngleWeight;

    [Tooltip("Maximum distance to target while hip firing")]
    [SerializeField] float maxDistanceHipFire = 10f;
    [Tooltip("Maximum distance to target while aiming down sights")]
    [SerializeField] float maxDistanceAiming = 25f;
    [Tooltip("Maximum angle to target while hip firing")]
    [SerializeField] float maxAngleHipFire = 35f;
    [Tooltip("Maximum angle to target while aiming down sights")]
    [SerializeField] float maxAngleAiming = 10f;

    [Tooltip("The Transform to source the forward direction from when aiming down sights, rather than the Player's forward.")]
    [SerializeField] Transform aimingFocusPoint;

    #endregion

    // Public API: manage active targets
    public static void AddActiveTarget(Target target)
    {
        if (!ALLTARGETS.Contains(target))
            ALLTARGETS.Add(target);
    }

    public static void RemoveActiveTarget(Target target)
    {
        if (ALLTARGETS.Contains(target))
            ALLTARGETS.Remove(target);
    }

    public static void ToggleAimingDownSights(bool value)
    {
        if(isAimingDownSights == value) return;
        isAimingDownSights = value;
        MaxDistance = value ? Instance.maxDistanceAiming : Instance.maxDistanceHipFire;
        MaxAngle = value ? Instance.maxAngleAiming : Instance.maxAngleHipFire;
        Aim = value ? Instance.aimingFocusPoint : Player.Transform; //AddCenterTransform later.
    }



    // Unity lifecycle
    private void Awake()
    {
        Instance = this;
        DistanceAngleWeight = Mathf.Clamp(distanceAngleWeighting, .1f, .9f);
        ToggleAimingDownSights(false);
    }

    // Main selection logic
    public static Target GetBestTarget()
    {
        int chosenIndex = -1;
        float closestScore = 5f; //Max possible score is 2 (1 distance + 1 angle)

        for (int i = 0; i < ALLTARGETS.Count; i++)
        {
            float distance = Vector3.Distance(Aim.position, ALLTARGETS[i].transform.position);
            float angle = Vector3.Angle(Aim.forward, ALLTARGETS[i].transform.position - Aim.position);

            if (ALLTARGETS[i] == null || ALLTARGETS[i].enabled == false || distance > MaxDistance || angle > MaxAngle) continue;

            float distanceScore = distance / MaxDistance;
            float angleScore = angle / MaxAngle;
            float finalScore = (distanceScore * DistanceAngleWeight) + (angleScore * (1f - DistanceAngleWeight));

            if (finalScore < closestScore)
            {
                closestScore = finalScore;
                chosenIndex = i;
            }
        }
        return chosenIndex != -1 ? ALLTARGETS[chosenIndex] : null;
    }
}

public class Target : MonoBehaviour
{

}