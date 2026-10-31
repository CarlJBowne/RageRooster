using System;
using System.Collections.Generic;
using System.Text;
using RageRooster.Core.Save;
using SLS.GeneralUtilities.Events;
using TMPro;
using UnityEngine;

namespace RageRooster.UI.HUD
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class CurrencyDisplay : MonoBehaviour
    {
        public TextMeshProUGUI text;

        int currentDisplay;
        int targetDisplay;

        private List<EventTicket> events;

        protected void Awake()
        {
            events = new()
            {
                SavedProgress.Active.Currency.Subscribe(SetCurrencyText)
            };
            InstantUpdate();
        }
        protected void OnDestroy() => events.DestroyAll();

        private void SetCurrencyText(int value) => targetDisplay = value;
        private void InstantUpdate() => text.text = SavedProgress.Active.Currency.Value.ToString();

        private void Update()
        {
            if (currentDisplay == targetDisplay) return;
            else if (targetDisplay > currentDisplay) currentDisplay++;
            else currentDisplay--;
            text.text = currentDisplay.ToString();
        }
    }
}
