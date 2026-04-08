using DG.Tweening;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System.Collections;
using UltEvents;
using UnityEngine;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerGrabber : MonoBehaviour
{
    // Grab-related configuration moved from PlayerRanged
    public float launchVelocity;
    public Transform twoHandedHand;
    public UltEvent<bool> GrabStateEvent;
    public Transform heldItemAnchor;
    public State dropLaunchState;
    public float turnToGrabRate;

    // Data
    public Grabbable currentGrabbed { get; private set; }
    private CoroutinePlus layerFadeCoroutine;


    private void OnEnable() { }
    private void OnDisable() { }

    private void LateUpdate()
    {
        // Make grabbed transform follow the held anchor (same behaviour as original)
        if (!enabled) return;
        if (currentGrabbed != null && heldItemAnchor != null)
        {
            var t = currentGrabbed.transform;
            t.SetPositionAndRotation(heldItemAnchor.position, heldItemAnchor.rotation);
        }
    }


    public void Grab(Grabbable target, bool warpTo = true)
    {
        Vector3 targetPos = target.transform.position;
        currentGrabbed = target;
        if (heldItemAnchor != null)
        {
            heldItemAnchor.localPosition = target.HeldOffset;
            target.transform.position = heldItemAnchor.position;
            target.transform.rotation = heldItemAnchor.rotation;
        }
        GrabStateEvent?.Invoke(true);
        SetGrabbingLayer(true);
        currentGrabbed.Grab();

        if(warpTo) Player.MovementBody.Position = targetPos;

        if (Player.StateMachine.Airborne && Upgrades.Active.dropLaunch && Input.Grab.IsPressed()) dropLaunchState.Enter();
    }


    public void Throw()
    {
        if(currentGrabbed == null) return;
        Vector3 throwDirection = Player.Forward;
        Vector3 throwVelocity = throwDirection * launchVelocity + Player.MovementBody.velocity;
        currentGrabbed?.Throw(throwVelocity);
        currentGrabbed = null;
        GrabStateEvent?.Invoke(false);
        SetGrabbingLayer(false);

    }

    public void Release(bool thrown)
    {
        currentGrabbed?.Release();
        currentGrabbed = null;
        GrabStateEvent?.Invoke(false);
        SetGrabbingLayer(false);
    }


    public void SetGrabbingLayer(bool value)
    {
        CoroutinePlus.Begin(ref layerFadeCoroutine, FadeLayers(value.Int(), 3), gameObject.activeInHierarchy ? this : Gameplay.Instance);
        IEnumerator FadeLayers(int target, float rate)
        {
            float current = Player.Animator.GetLayerWeight(2);
            while (!Mathf.Approximately(current, target))
            {
                current = Mathf.MoveTowards(current, target, Time.deltaTime * rate);
                SetBlend(current);
                yield return null;
            }
            SetBlend(target);
        }
        void SetBlend(float V)
        {
            Player.Animator.SetLayerWeight(2, V);
            Player.Animator.SetLayerWeight(3, V);
        }
    }




}