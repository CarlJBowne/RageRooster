using System;
using System.Collections;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public string upgradeName;






    private void OnTriggerEnter(Collider other)
    {
        Collect();
        Destroy(gameObject);
    }

    public void Collect()
    {
        FindObjectOfType<PlayerStateMachine>().SetUpgrade(upgradeName, true);
    }




}