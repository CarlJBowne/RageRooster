using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using SLS.GeneralUtilities.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RageRooster.UI.HUD
{
    [DefaultExecutionOrder(ExecutionOrders.UI)]
    public class HealthDisplay : MonoBehaviour
    {
        public List<Image> healthImages;
        public Sprite healthFullTexture;
        public Sprite healthEmptyTexture;

        int currentHealth = 3;
        int currentMaxHealth = 3;
        PlayerHealth PlayerHealth => RageRooster.Player.LocalContext.Player.Health;
        int absoluteMax => healthImages.Count;
        private List<EventTicket> events;
        //Sequence healthSequence;

        private void Awake()
        {
            events = new()
            {
                PlayerHealth.OnValueChanged.Subscribe(UpdateDisplay),
                PlayerHealth.OnMaxChanged.Subscribe(UpdateMax),
            };

            UpdateMax(PlayerHealth.Max);
            UpdateDisplay(PlayerHealth.Current);
        }
        private void OnDestroy() => events.DestroyAll();

        private void UpdateDisplay(int value)
        {
            currentHealth = value;
            for (int i = 0; i < currentMaxHealth && i < absoluteMax; i++)
                healthImages[i].sprite = i < currentHealth ? healthFullTexture : healthEmptyTexture;

        }
        private void UpdateMax(int value)
        {
            currentMaxHealth = value;
            for (int i = 0; i < absoluteMax; i++)
            {
                healthImages[i].gameObject.SetActive(i < currentMaxHealth);
                healthImages[i].sprite = i < currentHealth ? healthFullTexture : healthEmptyTexture;

                //else if (i >= currentMaxHealth)
                //{
                //    if (healthImages.Count <= i)
                //        healthImages.Add(Instantiate(healthImages[0].transform.parent, healthImages[0].transform.parent.parent).GetChild//(0).GetComponent<Image>());
                //    healthImages[i].enabled = true;
                //    healthImages[i].sprite = healthFullTexture;
                //
                //}
                //else if (i >= currentMaxHealth) healthImages[i].enabled = false;
            }
            //healthSequence?.Kill();
            //healthSequence = DOTween.Sequence();
            //float timeDelay = 0;
            //for (int j = 0; j < healthImages.Count; j++)
            //{
            //
            //
            //    HealthBarTween healthBarTween = healthImages[j].GetComponent<HealthBarTween>();
            //
            //    DOTween.Kill(healthImages[j].transform);
            //    healthImages[j].transform.localPosition = healthBarTween.origin;
            //    Tween tween =
            //    healthImages[j].transform.DOLocalMoveY(healthBarTween.origin.y - 50, 2f)
            //        .SetEase(Ease.InOutSine)
            //        .SetLoops(2, LoopType.Yoyo);
            //
            //    healthSequence.Insert(timeDelay, tween);
            //    timeDelay += 0.25f;
            //
            //
            //}
            //healthSequence.SetLoops(-1, LoopType.Restart);
        }

    }
}
