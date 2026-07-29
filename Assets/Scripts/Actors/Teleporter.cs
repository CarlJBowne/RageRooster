using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private Transform targetLocation;
    [SerializeField] private string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        ////Debug.Log("OnTriggerEnter called with: " + other.name); // Debug log

        if (other.CompareTag(targetTag))
        {
            ////Debug.Log("Teleporting " + other.name + " to " + targetLocation.position); // Debug log

            CharacterController characterController = other.GetComponent<CharacterController>();
            if (characterController != null)
            {
                ////Debug.Log("Disabling CharacterController for: " + other.name); // Debug log
                characterController.enabled = false; // Disable CharacterController
                other.transform.position = targetLocation.position; // Teleport
                characterController.enabled = true; // Re-enable CharacterController
                ////Debug.Log("Re-enabled CharacterController for: " + other.name); // Debug log
            }
            else
            {
                other.transform.position = targetLocation.position; // Teleport if no CharacterController
            }
        }
    }
}