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
    private int activeMaxHealth = 1;
    private float hintTimer;

    public int currentCurrency; //Consider putting this in its own system when we figure out what it does.


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
    }

    public void UpdateHealth(int currentValue, int maxValue)
    {
        if (maxValue > activeMaxHealth) 
            for (int i = 0; i < maxValue - activeMaxHealth; i++)
                healthImages.Add(Instantiate<Image>(healthImages[0], healthImages[0].transform.parent));
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

    public void AddCurrency(int currency)
    {
        currentCurrency += currency;
        currencyText.text = currentCurrency.ToString();
    }

}
