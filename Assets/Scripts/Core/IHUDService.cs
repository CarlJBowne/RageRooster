using System;

namespace RageRooster.Core
{
    public interface IHUDService
    {
        public static IHUDService HUD => Services.HUD;

        void ShowHint(string hintString);
        void SetHitMarkerVisibility(bool value);
        void UpdateHitMarker(UnityEngine.Vector3 position, float distance, bool hitDamagable);
    }
}
