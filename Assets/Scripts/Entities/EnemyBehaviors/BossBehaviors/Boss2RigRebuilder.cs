using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(-3), System.Obsolete]
public class Boss2RigRebuilder : MonoBehaviour
{
    private Dictionary<Rig, float> rigDefaultWeights = new();
    private Dictionary<IRigConstraint, float> constraintDefaultWeights = new();

    private Animator animator;
    private RigBuilder rigBuilder;

    void Awake()
    {
        TryGetComponent(out rigBuilder);
        TryGetComponent(out animator);
        CacheDefaultWeights();
    }

    void OnEnable()
    {
        StartCoroutine(Fuck());
    }

    private void CacheDefaultWeights()
    {
        if (rigBuilder == null) return;

        foreach (RigLayer layer in rigBuilder.layers)
        {
            if (layer.rig == null) continue;

            // Cache Rig default weight
            rigDefaultWeights[layer.rig] = layer.rig.weight;

            // Cache all constraint default weights
            IRigConstraint[] constraints = layer.rig.GetComponentsInChildren<IRigConstraint>(true);
            foreach (IRigConstraint constraint in constraints)
            {
                if (constraint is MonoBehaviour mb)
                {
                    PropertyInfo weightProp = constraint.GetType().GetProperty("weight", BindingFlags.Public | BindingFlags.Instance);
                    if (weightProp != null && weightProp.CanRead)
                    {
                        float defaultWeight = (float)weightProp.GetValue(constraint);
                        constraintDefaultWeights[constraint] = defaultWeight;
                    }
                }
            }
        }
    }

    private IEnumerator RestoreCachedWeights()
    {
        yield return null;

        foreach (KeyValuePair<Rig, float> kvp in rigDefaultWeights) kvp.Key.weight = kvp.Value;

        foreach (KeyValuePair<IRigConstraint, float> kvp in constraintDefaultWeights)
        {
            IRigConstraint constraint = kvp.Key;
            float defaultWeight = kvp.Value;
            //constraint.CreateJob(animator);

            PropertyInfo weightProp = constraint.GetType().GetProperty("weight", BindingFlags.Public | BindingFlags.Instance);
            if (weightProp != null && weightProp.CanWrite)
            {
                weightProp.SetValue(constraint, defaultWeight);
            }
            
        }
        animator.Rebind();
        rigBuilder.Build();
        animator.Update(0f);
    }

    IEnumerator Fuck()
    {
        yield return null;
        animator.Rebind();
        rigBuilder.Build(); 
        animator.Update(0f);
    }
}