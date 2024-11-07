using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BrightnessController : MonoBehaviour
{
    public Slider brightnessSlider;
    public Image brightnessOverlay;

    void OnEnable()
    {
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);
        brightnessSlider.value = savedBrightness;
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        InitializeBrightness();
    }

    void OnDisable()
    {
        brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
    }

    void OnBrightnessChanged(float value)
    {
        SetBrightness(value);
        PlayerPrefs.SetFloat("Brightness", value);
    }

    void InitializeBrightness()
    {
        SetBrightness(brightnessSlider.value);
    }

    void SetBrightness(float value)
    {
        float minAlpha = 0f;
        float maxAlpha = 0.9f;
        Color color = brightnessOverlay.color;
        color.a = Mathf.Lerp(maxAlpha, minAlpha, value);
        brightnessOverlay.color = color;

        Debug.Log($"Brightness value: {value}, Alpha: {color.a}");
    }
}