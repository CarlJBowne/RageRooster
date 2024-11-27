using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    private enum VolumeType
    {
        MASTER,
        MUSIC,
        SFX,
        AMBIENCE
    }

    [Header("Type")]
    [SerializeField] private VolumeType volumeType;
    private Slider volumeSlider;

    private void Awake()
    {
        volumeSlider = this.GetComponentInChildren<Slider>();
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat(volumeType.ToString(), 0.5f);
            volumeSlider.onValueChanged.AddListener(delegate { OnSliderValueChanged(); });
        }
        else
        {
            Debug.LogError("Slider component not found in children.");
        }
    }

    private void Start()
    {
        UpdateSliderValue();
    }

    private void UpdateSliderValue()
    {
        switch (volumeType)
        {
            case VolumeType.MASTER:
                volumeSlider.value = AudioManager.Get().masterVolume;
                break;
            case VolumeType.MUSIC:
                volumeSlider.value = AudioManager.Get().musicVolume;
                break;
            case VolumeType.SFX:
                volumeSlider.value = AudioManager.Get().SFXVolume;
                break;
            case VolumeType.AMBIENCE:
                volumeSlider.value = AudioManager.Get().ambienceVolume;
                break;
            default:
                Debug.LogError("Volume type not found: " + volumeType);
                break;
        }
    }

    public void OnSliderValueChanged()
    {
        switch (volumeType)
        {
            case VolumeType.MASTER:
                AudioManager.Get().masterVolume = volumeSlider.value;
                break;
            case VolumeType.MUSIC:
                AudioManager.Get().musicVolume = volumeSlider.value;
                break;
            case VolumeType.SFX:
                AudioManager.Get().SFXVolume = volumeSlider.value;
                break;
            case VolumeType.AMBIENCE:
                AudioManager.Get().ambienceVolume = volumeSlider.value;
                break;
            default:
                Debug.LogError("Volume type not found: " + volumeType);
                break;
        }

        PlayerPrefs.SetFloat(volumeType.ToString(), volumeSlider.value);
        PlayerPrefs.Save();
    }
}
