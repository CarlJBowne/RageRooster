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
            yield return Overlay.OverGameplay.BasicFadeOutWait(fadeoutTime);

            PostEnum().Begin(Gameplay.Instance);
            IEnumerator PostEnum()
            {
                yield return RoomManager.Transition(destination, forceFullTransition);
                Overlay.OverGameplay.BasicFadeIn(.5f);
            }
        }
        private IEnumerator CancelEnum()
        {
            yield return Overlay.OverGameplay.BasicFadeInWait(fadeoutTime);
        }






    }
}