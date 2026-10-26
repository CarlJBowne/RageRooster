using System;
using System.Collections.Generic;
using System.Text;
using SLS.MenuCore;
using SLS.Singletons;

namespace RageRooster.Core
{
    public abstract class HUD : Singleton.MonoBehaviour<HUD>
    {
        public static void ShowHint(string hint) => Get.DoShowHint(hint);

        protected abstract void DoShowHint(string hint);

        protected abstract void DoSetHitMarkerVisibility(bool value);
        public static void SetHitMarkerVisibility(bool value) => Get.DoSetHitMarkerVisibility(value);
    }
}