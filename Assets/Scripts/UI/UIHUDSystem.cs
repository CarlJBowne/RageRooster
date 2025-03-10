using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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

    private int activeMaxHealth = 1;
    private float hintTimer;
    private int currentCombo;

    // Called when the singleton instance is awakened
    protected override void OnAwake()
    {
        SetCurrencyText(GlobalState.currency.ToString());
        UpdateAmmo(GlobalState.maxAmmo);
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
        if (maxValue > activeMaxHealth)
            for (int i = 0; i < maxValue - activeMaxHealth; i++)
                healthImages.Add(Instantiate(healthImages[0], healthImages[0].transform.parent));
        activeMaxHealth = maxValue;
        //Note, can't go down since it probably won't ever go down.
        for (int i = 0; i < healthImages.Count; i++)
            healthImages[i].sprite = currentValue > i ? healthFullTexture : healthEmptyTexture;
    }

    // Displays a hint on the screen
    public void ShowHint(string hintString)
    {
        hintText.text = hintString;
        hintHolder.SetActive(true);
        hintTimer = hintTime;
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


}
