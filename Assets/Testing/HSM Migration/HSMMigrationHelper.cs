using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OLD = SLS.StateMachineV3;
using NEW = SLS.StateMachineH;
using UnityEditor;
using System.Linq;


public class HSMMigrationHelper : MonoBehaviour
{
    public OLD.State_OLD oldState;
    public NEW.State newState;

    public List<HSMMigrationHelper> children = new();


    //Does Step 1 (Adding New verisons of Scripts.)
    public void DoStep1(bool isStateMachine = false)
    {
        if (isStateMachine)
        {
            OLD.StateMachine_OLD oldStateMachine = GetComponent<OLD.StateMachine_OLD>();

            if (oldStateMachine.GetType() != typeof(OLD.StateMachine_OLD))
                newState = gameObject.GetOrAddComponent<NEW.StateMachine>();
        }
        else newState = gameObject.GetOrAddComponent<NEW.State>();
        EditorUtility.SetDirty(this);
        foreach (var child in children) child.DoStep1();
        if (isStateMachine && newState != null) (newState as NEW.StateMachine).Build();
    }

    //Does Step 2, (Adding default states for States with "separate from children" turned on, and then transfers all non-State related components over.)
    public void DoStep2(bool isStateMachine = false)
    {
        if (oldState.separateFromChildren)
        {
            // Create a new GameObject  
            GameObject newChild = new GameObject("Default");

            // Ensure it is the first child of this state's GameObject  
            newChild.transform.SetParent(transform, false);
            newChild.transform.SetSiblingIndex(0);

            // Add an OLD.State component to the new GameObject  
            var newChildState = newChild.AddComponent<OLD.State_OLD>();

            oldState.AddChildAtIndex0(newChildState);

            // Transfer non-State related components

            var baseComponents = new List<Component>(GetComponents<Component>());

            foreach (var component in baseComponents)
            {
                if (component is OLD.State_OLD or Transform) continue;
                var type = component.GetType();
                var newComponent = newChild.AddComponent(type);
                foreach (var field in type.GetFields())
                {
                    field.SetValue(newComponent, field.GetValue(component));
                }
                DestroyImmediate(component);
            }
            // Copy specific values from parent to child
            newChildState.signals = oldState.signals;
            newChildState.lockReady = oldState.lockReady;

            // Reset parent state values to default
            oldState.signals = new();
            oldState.lockReady = false;
            EditorUtility.SetDirty(newChild);
        }

        foreach (var child in children) child.DoStep2();
    }

    //Does Step 4 (If this State has any Signal data, a Signal Node is added to that State and the Key Value pairs are transferred over.)
    public void DoStep4(bool isStateMachine = false)
    {
        if (isStateMachine)
        {
            OLD.StateMachine_OLD SM = GetComponent<OLD.StateMachine_OLD>();
            NEW.SignalManager signalManager = gameObject.AddComponent<NEW.SignalManager>();
            if (SM.globalSignals != null && SM.globalSignals.Count > 0) 
                signalManager.globalSignals = ConvertFromYellowPaper(SM.globalSignals);
            EditorUtility.SetDirty(SM);
        }
        else
        {
            if (oldState.signals != null && oldState.signals.Count > 0)
            {
                NEW.SignalNode signalNode = gameObject.AddComponent<NEW.SignalNode>();
                signalNode.signals = ConvertFromYellowPaper(oldState.signals);
                EditorUtility.SetDirty(signalNode);
            }
        }
        EditorUtility.SetDirty(gameObject);

        foreach (var child in children)
        {
            child.DoStep4();
        }
    }

    private NEW.SignalSet ConvertFromYellowPaper(AYellowpaper.SerializedCollections.SerializedDictionary<string, UltEvents.UltEvent> input)
    {
        var output = new NEW.SignalSet();
        foreach (var pair in input)
        {
            output.Add(pair.Key, pair.Value);
        }
        return output;

    }

    //Does Step 5 (Removes all instances of OLD.States and OLD.StateMachines.)
    public void DoStep5(bool isStateMachine = false)
    {
        if (oldState != null) DestroyImmediate(oldState);
        EditorUtility.SetDirty(gameObject);
        foreach (var child in children) child.DoStep5();
    }

    //Does Step 6 (Removes all instances of HSMMigrationHelper and then builds the StateMachine.)
    public void DoStep6(bool isStateMachine = false)
    {
        // Recursively call DoStep6 on all children
        foreach (var child in children) child.DoStep6();

        GameObject GO = gameObject;

        // Remove HSMMigrationHelper from this GameObject
        DestroyImmediate(this);
        EditorUtility.SetDirty(gameObject);

        if (isStateMachine)
        {
            NEW.StateMachine newStateMachine = GetComponent<NEW.StateMachine>();
            newStateMachine.Build();
        }
    }


    public void RemoveDupes()
    {
        children = children.ToHashSet().ToList();
        EditorUtility.SetDirty(this);

        foreach (var child in children) child.RemoveDupes();
    }

    public void FixMissings()
    {
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] == null)
            {
                children[i] = HSMMigratorWindow.AddHelper(transform.GetChild(i).GetComponent<OLD.State_OLD>());
                EditorUtility.SetDirty(children[i]);
            }
        }
        foreach (var item in children)
        {
            item.FixMissings();
        }
    }

}