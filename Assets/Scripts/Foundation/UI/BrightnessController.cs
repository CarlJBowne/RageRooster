using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;

public class BrightnessController : MonoBehaviour
{
    public Slider brightnessSlider;
    public Image brightnessOverlay;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        brightnessSlider = FindInactiveSlider();
        brightnessOverlay = FindInactiveImage();
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);
        brightnessSlider.value = savedBrightness;
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        InitializeBrightness();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        brightnessSlider = FindInactiveSlider();
        brightnessOverlay = FindInactiveImage();
        if (brightnessSlider != null && brightnessOverlay != null)
        {
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            InitializeBrightness();
        }
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

        //Debug.Log($"Brightness value: {value}, Alpha: {color.a}");
    }

    Slider FindInactiveSlider()
    {
        Slider[] sliders = Resources.FindObjectsOfTypeAll<Slider>();
        foreach (Slider slider in sliders)
        {
            if (slider.gameObject.name == "BrightnessSlider")
            {
                return slider;
            }
        }
        return null;
    }

    Image FindInactiveImage()
    {
        Image[] images = Resources.FindObjectsOfTypeAll<Image>();
        foreach (Image image in images)
        {
            if (image.gameObject.name == "BrightnessOverlay")
            {
                return image;
            }
        }
        return null;
    }
}