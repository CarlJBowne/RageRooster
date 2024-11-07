using Cinemachine;
using UnityEngine;

public class FreeLookCollider : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera; // Assign your FreeLook camera in the Inspector
    public LayerMask obstacleLayers; // Assign the layers that the camera should collide with

    void Start()
    {
        // Check if the Cinemachine FreeLook Camera is assigned
        if (freeLookCamera != null)
        {
            // Check if the FreeLook Camera already has a Cinemachine Collider component
            var collider = freeLookCamera.GetComponent<CinemachineCollider>();

            if (collider == null) // If not, add the Cinemachine Collider component
            {
                collider = freeLookCamera.gameObject.AddComponent<CinemachineCollider>();
            }

            // Configure the Collider (you can adjust the parameters below)
            collider.m_CollideAgainst = obstacleLayers; // Set the layers for collision
            collider.m_MinimumDistanceFromTarget = 1f; // Adjust distance from obstacles
            collider.m_AvoidObstacles = true; // Enable obstacle avoidance
        }
        else
        {
            Debug.LogWarning("Cinemachine FreeLook Camera is not assigned!");
        }
    }
}
