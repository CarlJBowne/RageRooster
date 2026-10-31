using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;
using SLS.GeneralUtilities.Events;
using RageRooster.Core;
using RageRooster.Player;
using RageRooster.Core.Save;
using static RageRooster.Services;
using Player = RageRooster.Player.LocalContext;
using RageRooster.UI.HUD;

namespace RageRooster.UI
{
    [DefaultExecutionOrder(ExecutionOrders.UI)]
    public class HUDRoot : RageRooster.Core.HUD
    {

        public HealthDisplay Health;
        public AmmoDisplay Ammo;
        public HintDisplay Hint;
        public AimDisplay Aim;
        public CurrencyDisplay Currency;

        // Displays a hint on the screen
        protected override void DoShowHint(string hintString) => Hint.ShowHint(hintString);
        protected override void DoSetHitMarkerVisibility(bool value) => Aim.Active = value;
        protected override void DoUpdateHitMarker(Vector3 position, float distance, bool hitDamagable)
            => Aim.UpdateHitMarker(position, distance, hitDamagable);
    }

}