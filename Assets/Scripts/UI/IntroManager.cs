using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RawImage introVideoImage;
    [SerializeField] private Image titleScreenImage;
    [SerializeField] private RectTransform titleText;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Title Screen")]
    [SerializeField] private TextMeshProUGUI pressAnyKeyTMP;
    [SerializeField] private CanvasGroup rageRoosterCanvasGroup;

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float waitBetweenFades = 1f;
    [SerializeField] private float titleSlideDuration = 1.2f;
    [SerializeField] private float titleSlideOffset = 300f;
    [SerializeField] private float pressKeyFadeDelay = 1f;
    [SerializeField] private float pressKeyFadeDuration = 1f;
    [SerializeField] private float roosterFadeDuration = 1f;

    [Header("Press Any Key Pulse")]
    [SerializeField] private float pulseMinAlpha = 0.4f;
    [SerializeField] private float pulseMaxAlpha = 1f;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float glowMin = 0f;
    [SerializeField] private float glowMax = 1.5f;

    public InputActionReference continueButton;
    public InputActionReference skipButton;
    public TMPro.TextMeshProUGUI skipButtonNote;
    public DontDestroyMeOnLoad overlayPrefab;


    //private PlayerActions inputActions;
    private bool skipRequested = false;
    private bool waitingForTitleInput = false;
    private Coroutine pulseCoroutine;
    private Material pressKeyMaterialInstance;

    private void Awake()
    {
        continueButton.asset.Enable();
        //inputActions = new PlayerActions();
        //inputActions.Enable();
        skipButton.action.performed += SkipButtonPressed;

        if (Overlay.ActiveOverlays.Count == 0) Instantiate(overlayPrefab);
        if (SettingsMenu.brightnessOverlay == null) 
            SettingsMenu.brightnessOverlay = Overlay.OverMenus.transform.Find("BrightnessOverlay").GetComponent<Image>();

        var loadAttempt = SettingsMenu.GetTempSettings();
        if (loadAttempt != null && loadAttempt.HasValues)
        {
            
            AudioManager.Get().masterVolume = loadAttempt["V_Master"].As<float>();
            AudioManager.Get().musicVolume = loadAttempt["V_Music"].As<float>();
            AudioManager.Get().SFXVolume = loadAttempt["V_SFX"].As<float>();
            AudioManager.Get().ambienceVolume = loadAttempt["V_Amb"].As<float>();
            float brightness = loadAttempt["G_Brightness"].As<float>();

            SettingsMenu.brightnessOverlay.color = new(0, 0, 0, brightness);
        }

        SetAlpha(introVideoImage, 0f);
        SetAlpha(titleScreenImage, 0f);
        SetAlpha(pressAnyKeyTMP, 0f);

        if (rageRoosterCanvasGroup != null)
        {
            rageRoosterCanvasGroup.alpha = 0f;
            rageRoosterCanvasGroup.interactable = false;
            rageRoosterCanvasGroup.blocksRaycasts = false;
        }

        videoPlayer.loopPointReached += OnVideoFinished;

        if (pressAnyKeyTMP != null)
        {
            pressKeyMaterialInstance = new Material(pressAnyKeyTMP.fontMaterial);
            pressAnyKeyTMP.fontMaterial = pressKeyMaterialInstance;
            pressKeyMaterialInstance.SetFloat(ShaderUtilities.ID_GlowPower, glowMin);
        }

        StartCoroutine(PlayIntroSequence());
    }

    private void OnDestroy()
    {
        //inputActions.Disable();
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    /* private void Update()
     {
         if (waitingForTitleInput && inputActions.UI.AnyKey.WasPressedThisFrame())
         {
             waitingForTitleInput = false;

             if (pulseCoroutine != null)
                 StopCoroutine(pulseCoroutine);

             LoadMainMenu();
         }
     }*/

    private IEnumerator PlayIntroSequence()
    {
        // Skipping logos, go straight to video
        PlayVideo();
        yield return StartCoroutine(FadeImage(introVideoImage, 0f, 1f));
    }

    private void PlayVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(CrossFadeImages(introVideoImage, titleScreenImage));
    }

    private IEnumerator CrossFadeImages(Graphic fromImage, Graphic toImage)
    {
        float elapsed = 0f;
        SetAlpha(toImage, 0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetAlpha(fromImage, 1f - t);
            SetAlpha(toImage, t);
            yield return null;
        }

        SetAlpha(fromImage, 0f);
        SetAlpha(toImage, 1f);

        videoPlayer.Stop();
        fromImage.gameObject.SetActive(false);

        StartCoroutine(SlideInTitleText());
    }

    private IEnumerator SlideInTitleText()
    {
        Vector2 targetPos = titleText.anchoredPosition;
        Vector2 startPos = targetPos + new Vector2(0f, titleSlideOffset);

        float elapsed = 0f;
        titleText.anchoredPosition = startPos;

        while (elapsed < titleSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / titleSlideDuration);
            titleText.anchoredPosition = Vector2.Lerp(startPos, targetPos, EaseOutCubic(t));
            yield return null;
        }

        titleText.anchoredPosition = targetPos;

        if (rageRoosterCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(rageRoosterCanvasGroup, 0f, 1f, roosterFadeDuration));
        }

        yield return new WaitForSeconds(pressKeyFadeDelay);
        yield return StartCoroutine(FadeImage(pressAnyKeyTMP, 0f, 1f));

        pulseCoroutine = StartCoroutine(PulseTextAlpha(pressAnyKeyTMP));

        continueButton.action.performed += LoadMainMenu;

        //waitingForTitleInput = true;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private IEnumerator PulseTextAlpha(TextMeshProUGUI tmp)
    {
        float t = 0f;

        while (waitingForTitleInput)
        {
            t += Time.deltaTime * pulseSpeed;

            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(t * Mathf.PI) + 1f) / 2f);
            SetAlpha(tmp, alpha);

            if (pressKeyMaterialInstance != null)
            {
                float glow = Mathf.Lerp(glowMin, glowMax, (Mathf.Sin(t * Mathf.PI) + 1f) / 2f);
                pressKeyMaterialInstance.SetFloat(ShaderUtilities.ID_GlowPower, glow);
            }

            yield return null;
        }
    }

    private IEnumerator FadeImage(Graphic graphic, float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            //if (CheckForSkip()) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetAlpha(graphic, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(graphic, to);
    }

    //private bool CheckForSkip()
    //{
    //    if (!skipRequested && inputActions.UI.AnyKey.WasPressedThisFrame())
    //    {
    //        skipRequested = true;
    //    }
    //    return skipRequested;
    //}

    private void SkipToVideo()
    {
        SetAlpha(introVideoImage, 1f);
        SetAlpha(titleScreenImage, 1f);
        SetAlpha(pressAnyKeyTMP, 1f);

        videoPlayer.Play();

        if (rageRoosterCanvasGroup != null)
        {
            rageRoosterCanvasGroup.alpha = 1f;
            rageRoosterCanvasGroup.interactable = true;
            rageRoosterCanvasGroup.blocksRaycasts = true;
        }

        StartCoroutine(SlideInTitleText());
        waitingForTitleInput = true;
    }

    private bool pressedOnce;
    private void SkipButtonPressed(InputAction.CallbackContext ctx = default)
    {
        if (!pressedOnce)
        {
            skipButtonNote.gameObject.SetActive(true);
            pressedOnce = true;
        }
        else
        {
            LoadMainMenu();
        }
    }

    private void LoadMainMenu(InputAction.CallbackContext ctx = default)
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        //SettingsMenu.skipIntro = true;

        continueButton.action.performed -= LoadMainMenu;
        skipButton.action.performed -= SkipButtonPressed;

        SceneManager.LoadScene("MainMenu");
    }

    private void SetAlpha(Graphic graphic, float alpha)
    {
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
