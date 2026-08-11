using UnityEngine;
using SLS.StateMachineH;
using EditorAttributes;
using static RageRooster.Services;
using RageRooster.Core.Save;

public class Boss1Health : Health, IDamagable
{
    public float damageCooldown = 0.15f;
    public ColorTintAnimation damageTint;
    public int phase2Trigger;
    public UltEvents.UltEvent phase2Event;
    public UltEvents.UltEvent phase3Event;
    public State jumpState;
    public Transform phase2StartPos;
    public Transform phase3StartPos;

    public UltEvents.UltEvent ResetBossEvent;
    public UltEvents.UltEvent FinishBossEvent;
    public FlagClient<bool> finishedBossWorldChange;

    [HideInEditMode] public int bossPhase = 1;
    private bool phase2TriggerTriggered;
    private int stunCounter = 0;
    private Animator animator;
    private MovementAnimator moveAnim;
    private StateMachine machine;
    private Vector3 respawnPoint;
    private float lastDamageTime;

    protected override void Awake()
    {
        base.Awake();
        TryGetComponent(out animator);
        TryGetComponent(out moveAnim);
        TryGetComponent(out machine);
        respawnPoint = transform.position;
        if (finishedBossWorldChange.TryGet(out bool res) && res) FinishBossEvent?.Invoke();
    }

    private void OnEnable()
    {
        Player.OnRespawn += ResetBoss;
    }
    private void OnDestroy()
    {
        Player.OnRespawn -= ResetBoss;
    }

    protected override bool OverrideDamageable(Attack attack)
    {
        if (lastDamageTime + damageCooldown > Time.time) return false;
        if (attack[Attack.Tags.WeakSpot] && attack[Attack.Tags.GroundSlam])
        {
            damageTint.BeginAnimation();
            animator.Play("Damage");
            return true;
        }
        if (bossPhase != 2 && attack[Attack.Tags.WeakSpot] && attack[Attack.Tags.Egg] && machine.CurrentState.gameObject.name != "Charging")
        {
            stunCounter++;
            if (stunCounter > 2)
            {
                stunCounter = 0;
                machine.Signal("Charge");
            }
            else machine.Signal("Flinch");
            lastDamageTime = Time.time;
        }
        return false;
    }


    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        //if (!phase2TriggerTriggered && GetCurrentHealth() <= phase2Trigger) BeginPhase2();
    }

    public void OnDamageReturn()
    {
        if (!phase2TriggerTriggered && Current <= phase2Trigger) BeginPhase2();
        else machine.Signal("ReturnFromStun");
    }

    public void BeginPhase2()
    {
        moveAnim.SetTarget(phase2StartPos);
        jumpState.Enter();
        phase2TriggerTriggered = true;
        damagable = false;
        bossPhase = 2;
    }

    public void BeginPhase3()
    {
        moveAnim.SetTarget(phase3StartPos);
        jumpState.Enter();
        damagable = true;
        bossPhase = 3;
    }
    public void DoPhaseLand()
    {
        Transform dest = bossPhase switch
        {
            2 => phase2StartPos,
            3 => phase3StartPos,
            _ => null
        };
        GetComponent<Rigidbody>().MovePosition(dest.position + Vector3.up * 10f);
    }
    public void EndPhaseLand()
    {
        (bossPhase switch
        {
            2 => phase2Event,
            3 => phase3Event,
            _ => null
        })?.Invoke();
        moveAnim.SetTarget(Player.Transform);
    }

    public void ResetBoss()
    {
        if (finishedBossWorldChange.TryGet(out bool res) && res) return;
        if (!gameObject.activeSelf) return;
        transform.position = respawnPoint;
        GetComponent<Rigidbody>().MovePosition(respawnPoint);
        gameObject.SetActive(false);
        Current = Max;
        phase2TriggerTriggered = false;
        machine[0][0].Enter();
        animator.Play("Walking", -1, 0f);
        bossPhase = 1;
        damagable = true;
        ResetBossEvent?.Invoke();
        Player.OnRespawn -= ResetBoss;
    }

    public void FinishBoss()
    {
        finishedBossWorldChange.TrySet(true);
        FinishBossEvent?.Invoke();
    }


}