using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;

public class UIHUDSystem : Singleton<UIHUDSystem>
{
    public List<Image> healthImages;
    public Sprite healthFullTexture;
    public Sprite healthEmptyTexture;
    public GameObject hintHolder;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI currencyText;
    public float hintTime;
    public Timer.OneTime comboTime;
    public TextMeshProUGUI comboCounterText;
    public TextMeshProUGUI comboFlavorText;
    public ComboLevel[] comboLevels;
    public TextMeshProUGUI ammoText;
    public Image hitMarker;
    public Vector2 hitMarkerInputDistance;
    public Vector2 hitMarkerOutputScale;

    Canvas canvas;
    RectTransform canvasRect;
    Camera mainCamera;
    int activeMaxHealth = 1;
    float hintTimer;
    int currentCombo;

    Sequence healthBar;

    // Called when the singleton instance is awakened
    protected override void OnAwake()
    {
        SetCurrencyText(GlobalState.currency.ToString());
        mainCamera = Camera.main;
        transform.parent.TryGetComponent(out canvas);
        transform.parent.TryGetComponent(out canvasRect);
        PlayerHealth.Global.UI = this;
        PlayerRanged.Ammo.UI = this;
    }

    // Called every frame to update the HUD
    private void Update()
    {
        if (hintTimer > 0)
        {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0)
            {
                hintHolder.SetActive(false);
            }
        }
        comboTime.Tick(EndCombo);
    }

    // Updates the health bar based on current and maximum health values
    public void UpdateHealth(int currentValue, int maxValue)
    {
        for (int i = 0; i < activeMaxHealth || i < maxValue; i++)
        {
            if(i < activeMaxHealth && i < maxValue) 
                healthImages[i].sprite = currentValue > i ? healthFullTexture : healthEmptyTexture;
            else if (i >= activeMaxHealth)
            {
                if (healthImages.Count <= i) 
                    healthImages.Add(Instantiate(healthImages[0].transform.parent, healthImages[0].transform.parent.parent).GetChild(0).GetComponent<Image>());
                healthImages[i].enabled = true;
                healthImages[i].sprite = healthFullTexture;

            }
            else if (i >= maxValue) healthImages[i].enabled = false;
        }
        activeMaxHealth = maxValue;
        if(healthBar != null)
        {
            healthBar.Kill();
        }
        healthBar = DOTween.Sequence();
        float timeDelay = 0;
        for(int j = 0; j < healthImages.Count; j++)
        {


            HealthBarTween healthBarTween = healthImages[j].GetComponent<HealthBarTween>();

            DOTween.Kill(healthImages[j].transform);
            healthImages[j].transform.localPosition = healthBarTween.origin;
            Tween tween =
            healthImages[j].transform.DOLocalMoveY(healthBarTween.origin.y-50, 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(2, LoopType.Yoyo);

            healthBar.Insert(timeDelay, tween);
            timeDelay += 0.25f;


        }
        healthBar.SetLoops(-1, LoopType.Restart);


    }

    // Displays a hint on the screen
    public void ShowHint(string hintString)
    {
        hintText.text = HintTextParser(hintString);
        hintHolder.SetActive(true);
        hintTimer = hintTime;
    }

    public string HintTextParser(string input)
    {
        string result = string.Empty;
        //Split the whole text into parts based on the <> tags
        //Even numbers in the array are text, odd numbers are tags
        string[] subTexts = input.Split('<', '>');

        // textmeshpro still needs to parse its built-in tags, so we only include noncustom tags
        string displayText = "";
        for (int i = 0; i < subTexts.Length; i++)
        {
            if (i % 2 == 0)
                displayText += subTexts[i];
            else //Is Tag
            {
                string tag = subTexts[i].Replace(" ", "");
                if (tag.StartsWith("control="))
                {
                    string secondHalf = tag.Substring(8);
                    displayText += secondHalf switch
                    {
                        _ => "Null"
                    };
                }
                else displayText += $"<{tag}>";
            }
        }

        return result;
    }

    bool isCustomTag(string tag)
    {
        return tag.StartsWith("speed=") || tag.StartsWith("pause=") || tag.StartsWith("emotion=") || tag.StartsWith("action=");
    }


    // Sets the currency text on the HUD
    public static void SetCurrencyText(string currencyText) => Get().currencyText.text = currencyText;

    // Adds to the combo count
    public static void AddCombo() => Get().AddCombo_();
    private void AddCombo_()
    {
        currentCombo++;
        comboTime.Begin();
        comboCounterText.enabled = true;
        comboCounterText.text = currentCombo.ToString();
        if (currentCombo >= comboLevels[0].req)
        {
            int i = 0; //Cooler solution.
            for (; i < comboLevels.Length && currentCombo >= comboLevels[i + 1].req; i++) ;
            //int F = 0; //More sure solution.
            //for (int i = 1; i < comboLevels.Length && currentCombo >= comboLevels[i].req; i++) F = i;
            comboFlavorText.enabled = true;
            comboFlavorText.text = comboLevels[i].flavorText;
        }
    }

    // Ends the current combo
    private void EndCombo()
    {
        currentCombo = 0;
        comboTime.running = false;
        comboCounterText.enabled = false;
        comboFlavorText.enabled = false;
    }

    [Serializable]
    public struct ComboLevel
    {
        // Required combo count to reach this level
        public int req;
        // Flavor text for this combo level
        public string flavorText;
    }

    public void UpdateAmmo(int current)
    {
        ammoText.transform.parent.gameObject.SetActive(GlobalState.maxAmmo > 0);
        ammoText.text = $"{current}/{GlobalState.maxAmmo}";
    }


    public void SetHitMarkerVisibility(bool value) => hitMarker.enabled = value;

    public void UpdateHitMarker(Vector3 position, float distance, bool hitDamagable)
    {

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mainCamera.WorldToScreenPoint(position), null, out Vector2 canvasPos);
        hitMarker.rectTransform.anchoredPosition = canvasPos;
        hitMarker.transform.localScale = Vector3.one * Mathf.Lerp(hitMarkerOutputScale.x, hitMarkerOutputScale.y,
                                                Mathf.InverseLerp(hitMarkerInputDistance.x, hitMarkerInputDistance.y,
                                                    distance));
        hitMarker.color = new(1, 1, 1, hitDamagable ? 1 : .5f);
    }

}
