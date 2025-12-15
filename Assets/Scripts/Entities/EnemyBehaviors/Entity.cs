using System;
using System.Collections;
using UnityEngine;

[Tooltip("Manages behaviors meant to happen only when an entity is active.")]
public class Entity : MonoBehaviour
{
    public Behaviour[] disableComponents;
    public Action<States> onStateChanged;


    public void Awake()
    {
        enabled = base.enabled;
        currentState = enabled ? States.Default : States.Inactive;
    }


    public enum States
    {
        Inactive = -1,
        Default,
        Stunned,
        Grabbed,
        Thrown,
        RagDoll
    }

    private States currentState;

    public States State
    {
        get => base.enabled ? currentState : States.Inactive;
        set
        {
            currentState = value;

            void SetComponentsEnabled(bool enable)
            {
                if (disableComponents.Length > 0)
                    for (int i = 0; i < disableComponents.Length; i++)
                        if (disableComponents[i] != null)
                            disableComponents[i].enabled = enable;
            }
            
            SetComponentsEnabled(currentState switch
            {
                States.Inactive => false,
                States.Default => true,
                States.Stunned => false,
                States.Grabbed => false,
                States.Thrown => false,
                States.RagDoll => false,
                _ => enabled,
            });
            onStateChanged?.Invoke(currentState);
        }
    }
}

public interface IEntityComponent
{
    public Entity Entity { get; set; }
    public void StateChangeReceiver(Entity.States state);

    public static void Reset(IEntityComponent C)
    {
        var mb = C as MonoBehaviour;
        if (C.Entity == null)
        {
            var getAttempt = mb.GetComponent<Entity>();
            C.Entity = getAttempt != null ? getAttempt : mb.gameObject.AddComponent<Entity>();
        }
    }
    public static void Awake(IEntityComponent C) 
    {
        C.Entity.onStateChanged += C.StateChangeReceiver;
    }
}