using RageRooster.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.MenuCore;

namespace RageRooster.RoomSystem.MovementSystems
{
    public class TunnelTransition : MonoBehaviour
    {
        public Destination destination;
        public float fadeoutTime = 1f;
        public bool cancellable;
        public bool forceFullTransition;
        public bool lockCameraPosition;
        public bool lockCameraRotation;

        private bool playerWithin;
        Coroutine coroutine;
        Music.Channel activeMusicChannel;

        private void OnTriggerEnter(Collider other)
        {
            if (!playerWithin && other == Player.Collider)
            {
                playerWithin = true;
                if (lockCameraPosition || lockCameraRotation) Cameras.LockPrimary(lockCameraPosition, lockCameraRotation);
                coroutine?.StopAuto();
                coroutine = new(TransitionEnum(), this);
                activeMusicChannel = Music.Primary;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if(playerWithin && other == Player.Collider && cancellable)
            {
                playerWithin = false;
                if (lockCameraPosition || lockCameraRotation) Cameras.LockPrimary(false, false);
                coroutine?.StopAuto();
                coroutine = new(CancelEnum(), this);
            }
        }

        private IEnumerator TransitionEnum()
        {
            yield return Overlay.UnderHUD.FadeAlpha(1, fadeoutTime);

            RoomManager.TransitionStyle = new()
            {
                forceFullTransition = forceFullTransition,
                FadeOutRoutine = Overlay.BetweenUI.FadeAlpha(1, .1f),
                FadeInRoutine = Overlay.BetweenUI.FadeAlpha(0, .5f),
                PostFadeOutAction = Overlay.UnderHUD.ResetState,
            };
            RoomManager.StartTransition(destination);
        }
        private IEnumerator CancelEnum()
        {
            yield return Overlay.UnderHUD.FadeAlpha(0, fadeoutTime);
            activeMusicChannel = null;
        }






    }
}