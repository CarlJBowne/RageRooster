using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RageRooster.Core
{
    public interface IOverlayTopPlus
    {
        public static IOverlayTopPlus Self => RageRooster.Services.OverlayTopPlus;
        public static bool Present => Self != null;

        public void LoadingPopup(bool value = true);
        public IEnumerator GameOverAnimation(float duration = 1f);
    }
}
