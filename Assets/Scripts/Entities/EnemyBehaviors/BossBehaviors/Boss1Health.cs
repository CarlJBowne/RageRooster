using UnityEngine;
using SLS.StateMachineV3;
using EditorAttributes;

public class Boss1Health : Health
{
    public float damageCooldown = 0.15f;
    public int phase2Trigger;
    public UltEvents.UltEvent phase2Event;
    public UltEvents.UltEvent phase3Event;
    public State jumpState;
    public Transform phase2StartPos;
    public Transform phase3StartPos;

    public UltEvents.UltEvent ResetBossEvent;
    public UltEvents.UltEvent FinishBossEvent;
    public WorldChange finishedBossWorldChange;

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
        if (finishedBossWorldChange.Enabled) FinishBossEvent?.Invoke();
    }

    protected override bool OverrideDamageable(Attack attack)
    {
        if (lastDamageTime + damageCooldown > Time.time) return false;
        if (attack.HasTag("OnWeakSpot") && attack.HasTag("GroundSlam"))
        {
            animator.Play("Damage");
            return true;
        }
        if (bossPhase != 2 && attack.HasTag("InEyes") && attack.HasTag("Egg"))
        {
            stunCounter++; 
            if (stunCounter > 2)
            {
                stunCounter = 0;
                machine.SendSignal("Charge");
            }
            else machine.SendSignal("Flinch"); 
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
        if (!phase2TriggerTriggered && GetCurrentHealth() <= phase2Trigger) BeginPhase2();
        else machine.SendSignal("ReturnFromStun");
    }

    public void BeginPhase2()
    {
        moveAnim.SetTarget(phase2StartPos);
        jumpState.TransitionTo();
        phase2TriggerTriggered = true;
        damagable = false;
        bossPhase = 2;
    }

    public void BeginPhase3()
    {
        moveAnim.SetTarget(phase3StartPos);
        jumpState.TransitionTo();
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
        moveAnim.SetTarget(Gameplay.Player.transform);
    }

    public void ResetBoss()
    {
        if (finishedBossWorldChange.Enabled) return;
        if (!gameObject.activeSelf) return;
        transform.position = respawnPoint;
        GetComponent<Rigidbody>().MovePosition(respawnPoint);
        gameObject.SetActive(false);
        health = maxHealth;
        phase2TriggerTriggered = false;
        machine[0][0].TransitionTo();
        ResetBossEvent?.Invoke();
    }

    public void FinishBoss()
    {
        finishedBossWorldChange.Enable();
        FinishBossEvent?.Invoke();
    }


}