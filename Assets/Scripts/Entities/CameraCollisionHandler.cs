using UnityEngine;
using Cinemachine;
/// <summary>
/// Add the CinemachineCollider component to the Cinemachine Virtual Camera.
/// Configure the CinemachineCollider settings to handle collisions.
/// Attach the CameraCollisionHandler script to your Cinemachine Virtual Camera.
/// Adjust the CinemachineCollider settings in the script or via the Unity Inspector to fit your needs.
/// </summary>
public class CameraCollisionHandler : MonoBehaviour
{
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineCollider collider;

    void Start()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            collider = virtualCamera.GetComponent<CinemachineCollider>();
            if (collider == null)
            {
                collider = virtualCamera.gameObject.AddComponent<CinemachineCollider>();
            }

            // Configure the collider settings
            collider.m_Damping = 0.5f; // Adjust damping as needed
            collider.m_MinimumDistanceFromTarget = 1.0f; // Adjust minimum distance as needed
            collider.m_AvoidObstacles = true;
            collider.m_DistanceLimit = 10.0f; // Adjust distance limit as needed
        }
    }
}