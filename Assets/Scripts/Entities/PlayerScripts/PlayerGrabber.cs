using DG.Tweening;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System.Collections;
using UltEvents;
using UnityEngine;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerGrabber : MonoBehaviour, IGrabber
{
    // Grab-related configuration moved from PlayerRanged
    public float launchVelocity;
    public Transform twoHandedHand;
    public UltEvent<bool> GrabStateEvent;
    public Transform heldItemAnchor;
    public State dropLaunchState;
    public float turnToGrabRate;

    // Data
    public IGrabbable currentGrabbed { get; private set; }
    public Collider ownerCollider => _collider;

    private CoroutinePlus layerFadeCoroutine;
    private Collider _collider;
    private PlayerInteracter interacter = null;

    private void Awake()
    {
        TryGetComponent(out _collider);
        // cache interacter via singleton or GetComponent if present
    }

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


    public void OfficialGrab(IGrabbable target)
    {
        currentGrabbed = target;
        
    }




    #region OLD
    public void TryGrabThrow(PlayerGrabAction state, State throwState)
    {
        if (!Player.StateMachine.SignalManager.Locked && currentGrabbed != null)
        {
            throwState.Enter();
            new CoroutinePlus(QuickTurn(), this);
            IEnumerator QuickTurn()
            {
                float time = 0f;
                // Use pointer rotation if needed via PlayerRanged; but to avoid tight coupling use Player.MovementBody.transform.forward fallback
                // Angle calculation uses transforms; best-effort (keeps original intent)
                float rate = 360f / .1f; // fallback fast rate
                while (time < 0.1f)
                {
                    time += Time.deltaTime;
                    // rotate Player.MovementBody towards its forward (no-op fallback) to mimic same timing
                    Player.MovementBody.RotationQ = Quaternion.RotateTowards(Player.MovementBody.RotationQ, Player.MovementBody.transform.rotation, rate * Time.deltaTime);
                    yield return null;
                }
            }
        }
        else
        {
            // Begin grab attempt using the interacter helper (if available)
            if (interacter != null)
                state.BeginGrabAttempt(interacter.HasUsableGrabbable());
            else
                state.BeginGrabAttempt(PlayerInteracter.Instance.HasUsableGrabbable(out IGrabbable g) ? g : null);
        }
    }

    public void TryGrabThrowAir(PlayerGrabAction state)
    {
        if (currentGrabbed != null)
        {
            // quick Player.MovementBody rotate like original
            Player.MovementBody.transform.DOBlendableRotateBy(new Vector3(0, 0, 0), 0.1f);
            // choose appropriate throw state, original used Upgrades.Active; preserve external dependency by checking
            var chosen = !Upgrades.Active.dropLaunch ? Player.StateMachine.controller.ranged.airThrowState : Player.StateMachine.controller.ranged.dropLaunchState;
            chosen.Enter();
        }
        else
        {
            if (interacter != null)
                state.BeginGrabAttempt(interacter.HasUsableGrabbable());
            else
                state.BeginGrabAttempt(PlayerInteracter.Instance.HasUsableGrabbable(out IGrabbable g) ? g : null);
        }
    }

    public void GrabPoint(IGrabbable grabbed)
    {
        if (!grabbed.Grab(this)) return;
        currentGrabbed = grabbed;

        CoroutinePlus.Begin(ref layerFadeCoroutine, TurnOnLayers(1f), this);
        IEnumerator TurnOnLayers(float rate)
        {
            float V = 0;
            while (V < 1)
            {
                V += Time.deltaTime * rate;
                if (Player.Animator != null)
                {
                    Player.Animator.SetLayerWeight(2, V);
                    Player.Animator.SetLayerWeight(3, V);
                }
                yield return null;
            }
        }

        // position grabbed object at anchor
        if (heldItemAnchor != null)
        {
            heldItemAnchor.localPosition = grabbed.HeldOffset;
            grabbed.transform.position = heldItemAnchor.position;
            grabbed.transform.rotation = heldItemAnchor.rotation;
        }

        GrabStateEvent?.Invoke(true);
    }

    public void GrabPointSignal()
    {
        if (Player.StateMachine != null)
            Player.StateMachine.SendSignal(new("FinishGrab", ignoreLock: true));
    }

    public void ThrowPoint()
    {
        // When throwing in-air with drop launch, the original invoked jumpState.BeginJump() - that lives on PlayerRanged.
        // We attempt to call it via the state Player.StateMachine controller if available.
        if (Player.StateMachine?.controller?.ranged != null)
        {
            var ranged = Player.StateMachine.controller.ranged;
            if (!Player.MovementBody.Grounded && Upgrades.Active.dropLaunch)
            {
                ranged.jumpState.BeginJump();
            }

            Vector3 direction =
                ranged.aimingState.State
                ? ranged.pointer.startV.forward
                : !Player.MovementBody.Grounded && Upgrades.Active.dropLaunch
                    ? Vector3.down
                    : Player.MovementBody.transform.forward;

            Release(direction * launchVelocity, true);
        }
        else
        {
            // fallback: forward from Player.MovementBody
            Release(Player.MovementBody.transform.forward * launchVelocity, true);
        }
    }

    public void Release(Vector3 velocity, bool thrown = false)
    {
        if (thrown && currentGrabbed != null) currentGrabbed.Throw(velocity);
        else if (currentGrabbed != null) currentGrabbed.Release();

        CoroutinePlus.Begin(ref layerFadeCoroutine, TurnOffLayers(1f), gameObject.activeInHierarchy ? this : Gameplay.Instance);
        IEnumerator TurnOffLayers(float rate)
        {
            float V = 1;
            while (V > 0)
            {
                V -= Time.deltaTime * rate;
                if (Player.Animator != null)
                {
                    Player.Animator.SetLayerWeight(2, V);
                    Player.Animator.SetLayerWeight(3, V);
                }
                yield return null;
            }
        }

        currentGrabbed = null;
        GrabStateEvent?.Invoke(false);
    }

    public IGrabbable CheckForGrabbable()
    {
        IGrabbable.Test(Physics.OverlapSphere(transform.position + GetRealOffset(transform), checkSphereRadius, layerMask), out IGrabbable result);
        return result;
    }

    private Vector3 GetRealOffset(Transform t) =>
        t.forward * checkSphereOffset.z + t.up * checkSphereOffset.y + t.right * checkSphereOffset.x;

    public float checkSphereRadius;
    public Vector3 checkSphereOffset;
    public LayerMask layerMask;


    #endregion
}