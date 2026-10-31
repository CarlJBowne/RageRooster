using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RageRooster.UI.HUD
{
    [RequireComponent(typeof(Image))]
    public class AimDisplay : MonoBehaviour
    {
        public Image image;

        public Vector2 hitMarkerInputDistance = new(2, 10.5f);
        public Vector2 hitMarkerOutputScale = new(1.6f, .45f);

        public bool Active { set => gameObject.SetActive(value); }

        RectTransform canvasRect;
        Camera mainCamera;

        protected void Awake()
        {
            mainCamera = Camera.main;
            canvasRect = image.canvas.GetComponent<RectTransform>();
            Active = false;
        }




        public void UpdateHitMarker(Vector3 position, float distance, bool hitDamagable)
        {

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mainCamera.WorldToScreenPoint(position), null, out Vector2 canvasPos);
            image.rectTransform.anchoredPosition = canvasPos;
            transform.localScale = Vector3.one * Mathf.Lerp(hitMarkerOutputScale.x, hitMarkerOutputScale.y,
                                                    Mathf.InverseLerp(hitMarkerInputDistance.x, hitMarkerInputDistance.y,
                                                        distance));
            image.color = new(1, 1, 1, hitDamagable ? 1 : .5f);
        }



    }
}
