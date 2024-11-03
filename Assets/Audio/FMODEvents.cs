using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    
    [field: Header("Player SFX")]
    [field: SerializeField] public EventReference playerJump { get; private set; }
    [field: SerializeField] public EventReference playerLand { get; private set; }
    [field: SerializeField] public EventReference playerDeath { get; private set; }
    
    [field: Header("Music")]
    [field: SerializeField] public EventReference music { get; private set; }
    [field: Header("SFX")]
    [field: SerializeField] public EventReference laserShot { get; private set; }
    [field: Header("UI")]
    [field: SerializeField] public EventReference buttonClick { get; private set; }
    [field: Header("Ambience")]
    [field: SerializeField] public EventReference ambience { get; private set; }


    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Found more than one FMOD Event in the scene.");
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (ambience.Guid == System.Guid.Empty)
        {
            Debug.LogError("Ambience event is not assigned in the FMODEvents component.");
        }
    }
}
