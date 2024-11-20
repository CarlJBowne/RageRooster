using System;
using System.Collections;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType
    {
        Coin,
        Hint,
        Upgrade,
        Hen
    }

    public ItemType type;
    public string upgradeName;
    [TextArea]
    public string hintString;
    public int coinAmount = 1;




    private void OnTriggerEnter(Collider other)
    {
        if(type == ItemType.Coin)
        {
            UIHUDSystem.Get().AddCurrency(coinAmount);
        }
        else if (type == ItemType.Hint)
        {
            UIHUDSystem.Get().ShowHint(hintString);
        }
        else if (type == ItemType.Upgrade)
        {
            FindObjectOfType<PlayerStateMachine>().SetUpgrade(upgradeName, true);
        }
        else if(type == ItemType.Hen)
        {

        }

        if (type != ItemType.Hint) Destroy(gameObject);

    }




}