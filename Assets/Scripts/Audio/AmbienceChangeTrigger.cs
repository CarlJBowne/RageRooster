using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class AmbienceChangeTrigger : MonoBehaviour
{
    [Header("Parameter Change")]
    [SerializeField] private AmbienceType ambienceType;
    [SerializeField] private float parameterValue;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EventReference ambienceEvent = GetAmbienceEvent(ambienceType);
            AudioManager.Get.SetAmbienceParameter(
#if UNITY_EDITOR
                ambienceEvent.Path,
#else
"PROBLEM",
#endif
                parameterValue);
        }
    }

    private EventReference GetAmbienceEvent(AmbienceType type)
    {
        switch (type)
        {
            case AmbienceType.IreGorge:
                return FMODEvents.Get.ireGorgeAmbience;
            case AmbienceType.RockyFurrows:
                return FMODEvents.Get.rockyFurrowsAmbience;
            case AmbienceType.WaterSplash:
                return FMODEvents.Get.waterSplash;
            case AmbienceType.Boss:
                return FMODEvents.Get.bossAmbience;
            case AmbienceType.Transition:
                return FMODEvents.Get.transitionAmbience;
            default:
                return new EventReference();
        }
    }
}

public enum AmbienceType
{
    IreGorge,
    RockyFurrows,
    WaterSplash,
    Boss,
    Transition
}
