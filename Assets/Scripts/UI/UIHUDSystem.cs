using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;
using RageRooster.Core;
using static RageRooster.Services;
using RageRooster.Player;
using RageRooster.Core.Save;

[DefaultExecutionOrder(ExecutionOrders.GameplaySystems+20)]
public class UIHUDSystem : MonoBehaviour
{
    public GameObject hintHolder;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI currencyText;
    public float hintTime;
    public Image hitMarker;
    public Vector2 hitMarkerInputDistance;
    public Vector2 hitMarkerOutputScale;

    Canvas canvas;
    RectTransform canvasRect;
    Camera mainCamera;
    float hintTimer;

    public static UIHUDSystem Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        //Services.
        {

        }

        mainCamera = Camera.main;
        transform.parent.TryGetComponent(out canvas);
        transform.parent.TryGetComponent(out canvasRect);

        health.Init();
        ammo.Init();
        SetCurrencyText(SavedProgress.Active.Currency);
    }

    protected void OnDestroy()
    {

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
        combo.comboTime.Tick(combo.EndCombo);
    }

    public Health health;
    [Serializable]
    public class Health
    {
        public List<Image> healthImages;
        public Sprite healthFullTexture;
        public Sprite healthEmptyTexture;

        int currentHealth = 1;
        int maxHealth = 1;

        Sequence healthBar;

        public void Init()
        {
            UpdateMax(PlayerStats.Active.MaxHealth);
            Update(maxHealth);
        }

        public void Update(int value)
        {
            currentHealth = value;
            for (int i = 0; i < maxHealth; i++)
                healthImages[i].sprite = value > i ? healthFullTexture : healthEmptyTexture;
        }
        public void UpdateMax(int value)
        {
            maxHealth = value;
            for (int i = 0; i < maxHealth || i < maxHealth; i++)
            {
                if (i < maxHealth && i < maxHealth)
                    healthImages[i].sprite = currentHealth > i ? healthFullTexture : healthEmptyTexture;
                else if (i >= maxHealth)
                {
                    if (healthImages.Count <= i)
                        healthImages.Add(Instantiate(healthImages[0].transform.parent, healthImages[0].transform.parent.parent).GetChild(0).GetComponent<Image>());
                    healthImages[i].enabled = true;
                    healthImages[i].sprite = healthFullTexture;

                }
                else if (i >= maxHealth) healthImages[i].enabled = false;
            }
            healthBar?.Kill();
            healthBar = DOTween.Sequence();
            float timeDelay = 0;
            for (int j = 0; j < healthImages.Count; j++)
            {


                HealthBarTween healthBarTween = healthImages[j].GetComponent<HealthBarTween>();

                DOTween.Kill(healthImages[j].transform);
                healthImages[j].transform.localPosition = healthBarTween.origin;
                Tween tween =
                healthImages[j].transform.DOLocalMoveY(healthBarTween.origin.y - 50, 2f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(2, LoopType.Yoyo);

                healthBar.Insert(timeDelay, tween);
                timeDelay += 0.25f;


            }
            healthBar.SetLoops(-1, LoopType.Restart);
        }
    }

    public readonly Ammo ammo = new();
    [Serializable]
    public class Ammo
    {
        public TextMeshProUGUI ammoText;
        int currentAmmo = 0;
        int maxAmmo = 0;
        public void Init()
        {
            UpdateMax(PlayerStats.Active.MaxAmmo);
            Update(maxAmmo);
        }
        public void Update(int value)
        {
            currentAmmo = value;
            ammoText.text = $"{currentAmmo}/{maxAmmo}";
        }
        public void UpdateMax(int value)
        {
            maxAmmo = value;
            ammoText.transform.parent.gameObject.SetActive(value > 0);
            ammoText.text = $"{currentAmmo}/{value}";
        }
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
        for (int i = 0; i < subTexts.Length; i++)
        {
            if (i % 2 == 0)
                result += subTexts[i];
            else //Is Tag
            {
                string tag = subTexts[i].Replace(" ", "");
                if (tag.StartsWith("control="))
                    result += RemappingMenu.GetControlString(tag.Substring(8));
                else result += $"<{tag}>";
            }
        }

        return result;
    }

    bool isCustomTag(string tag)
    {
        return tag.StartsWith("speed=") || tag.StartsWith("pause=") || tag.StartsWith("emotion=") || tag.StartsWith("action=");
    }


    // Sets the currency text on the HUD
    public void SetCurrencyText(int amount)
    {
        if (IPlayer.Self == null) return;
        currencyText.text = amount.ToString();
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






    [SerializeField] private Combo combo;
    [Serializable]
    public class Combo
    {
        public Timer.OneTime comboTime;
        public TextMeshProUGUI comboCounterText;
        public TextMeshProUGUI comboFlavorText;
        public ComboLevel[] comboLevels;

        int currentCombo;

        // Adds to the combo count
        public static void AddCombo() => Instance.combo.AddCombo_();
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
        public void EndCombo()
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

    }


}
