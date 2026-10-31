using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using RageRooster.Player;
using SLS.GeneralUtilities.Events;
using TMPro;
using UnityEngine;

namespace RageRooster.UI
{
    [DefaultExecutionOrder(ExecutionOrders.UI), RequireComponent(typeof(TextMeshProUGUI))]
    public class AmmoDisplay : MonoBehaviour
    {
        public TextMeshProUGUI ammoText;

        int currentAmmo = 0;
        int currentMaxAmmo = 0;
        PlayerRanged PlayerRanged => Player.LocalContext.Player.Ranged;
        private List<EventTicket> events;

        private void Awake()
        {
            events = new()
            {
                PlayerRanged.Ammo.Subscribe(UpdateDisplay),
                PlayerRanged.Ammo.SubscribeMax(UpdateMax),
            };
            UpdateMax(PlayerRanged.Ammo.Max);
            UpdateDisplay(PlayerRanged.Ammo.Value);
        }
        private void OnDestroy() => events.DestroyAll();

        public void UpdateDisplay(int value)
        {
            currentAmmo = value;
            ammoText.text = $"{currentAmmo}/{currentMaxAmmo}";
        }
        public void UpdateMax(int value)
        {
            currentMaxAmmo = value;
            ammoText.transform.parent.gameObject.SetActive(value > 0);
            ammoText.text = $"{currentAmmo}/{value}";
        }


    }
}
