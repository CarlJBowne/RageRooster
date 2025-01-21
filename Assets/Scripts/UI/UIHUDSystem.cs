using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UIHUDSystem : Singleton<UIHUDSystem>
{
    public List<Image> healthImages; //Make Array later for performance
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

    private int activeMaxHealth = 1;
    private float hintTimer;
    private int currentCombo;

    protected override void OnAwake()
    {
        currencyText.text = "0";
    }

    private void Update()
    {
        if (hintTimer > 0)
        {
            hintTimer -= Time.deltaTime;
            if(hintTimer <= 0)
            {
                hintHolder.SetActive(false);
            }
        }
        comboTime.Tick(EndCombo);
    }

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

    public void ShowHint(string hintString)
    {
        hintText.text = hintString;
        hintHolder.SetActive(true);
        hintTimer = hintTime;
    }

    public static void SetCurrencyText(string currencyText) => Get().currencyText.text = currencyText;

    public static void AddCombo() => Get().AddCombo_();
    private void AddCombo_()
    {
        currentCombo++;
        comboTime.Begin();
        comboCounterText.enabled = true;
        comboCounterText.text = currentCombo.ToString();
        if(currentCombo >= comboLevels[0].req)
        {
            int i = 0; //Cooler solution.
            for (; i < comboLevels.Length && currentCombo >= comboLevels[i + 1].req; i++);
            //int F = 0; //More sure solution.
            //for (int i = 1; i < comboLevels.Length && currentCombo >= comboLevels[i].req; i++) F = i;
            comboFlavorText.enabled = true;
            comboFlavorText.text = comboLevels[i].flavorText;
        }
    }
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
        public int req;
        public string flavorText;
    }

}
