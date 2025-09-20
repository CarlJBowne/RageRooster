using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem.MovementSystems
{
    public class TunnelTransition : MonoBehaviour
    {
        public Destination destination;
        public float fadeoutTime = 1f;
        public bool cancellable;
        public bool forceFullTransition;

        private bool playerWithin;
        CoroutinePlus coroutine;
        UnityEngine.UI.Image blackout;

        private void Awake() => blackout = Overlay.OverGameplay.blackout;

        private void OnTriggerEnter(Collider other)
        {
            if (!playerWithin && other == Player.Collider)
            {
                playerWithin = true;
                coroutine?.StopAuto();
                coroutine = new(TransitionEnum(), this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if(playerWithin && other == Player.Collider && cancellable)
            {
                playerWithin = false;
                coroutine?.StopAuto();
                coroutine = new(CancelEnum(), this);
            }
        }

        private IEnumerator TransitionEnum()
        {
            Overlay.OverGameplay.SetAnimated(false);
            while(blackout.color.a < 1f)
            {
                blackout.color = new Color(0, 0, 0, Mathf.Min(1f, blackout.color.a + (Time.unscaledDeltaTime / fadeoutTime)));
                yield return null;
            }

            RoomManager.Transition(destination, forceFullTransition).Begin(Overlay.OverGameplay);
            Overlay.OverGameplay.SetAnimated(true);
        }
        private IEnumerator CancelEnum()
        {
            Overlay.OverGameplay.SetAnimated(false);
            while (blackout.color.a > 0f)
            {
                blackout.color = new Color(0, 0, 0, Mathf.Min(1f, blackout.color.a - (Time.unscaledDeltaTime / fadeoutTime)));
                yield return null;
            }
            Overlay.OverGameplay.SetAnimated(true);
        }






    }
}