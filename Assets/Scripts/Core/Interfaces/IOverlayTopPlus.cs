using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RageRooster.Core
{
    public interface IOverlayTopPlus
    {
        public static IOverlayTopPlus Self => RageRooster.Services.UI.OverlayTopPlus;
        public static bool Present => Self != null;

        public SLS.MenuCore.Overlay Overlay => Self as SLS.MenuCore.Overlay;
        public void LoadingPopup(bool value = true);
        public IEnumerator GameOverAnimation(float duration = 1f);
    }
}
