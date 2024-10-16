using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class tracking designer-defined variables kept across the whole StateMachine. <br />
/// Basic and untested. Possibly not fully functional.<br />
/// Available Types include: Bool, Int, Float, Vector2, Vector3, String, Char.
/// </summary>
public class StateMachineVariables : MonoBehaviour
{

    [Serializable]
    public struct VarEntry
    {
        public enum VariableType { Bool, Int, Float, Vector2, Vector3, String, Char }

        public string name;
        public VariableType type;
    }

    [SerializeField] VarEntry[] variables;
    private Dictionary<string, object> Vars;
    private bool init;

    /// <summary>
    /// Initializes the Variables. Should only be called by StateMachine.
    /// </summary>
    internal void Initialize()
    {
        if (init) return;
        Vars = new Dictionary<string, object>();
        for (int i = 0; i < variables.Length; i++)
        {
            Type targetType = variables[i].type switch
            {
                VarEntry.VariableType.Bool => typeof(bool),
                VarEntry.VariableType.Int => typeof(int),
                VarEntry.VariableType.Float => typeof(float),
                VarEntry.VariableType.Vector2 => typeof(Vector2),
                VarEntry.VariableType.Vector3 => typeof(Vector3),
                VarEntry.VariableType.String => typeof(string),
                VarEntry.VariableType.Char => typeof(char),
                _ => null
            };
            if (targetType == null) return;
            Vars.Add(variables[i].name, Activator.CreateInstance(targetType));
        }
        init = true;
    }

    /// <summary>
    /// Sets the value of a variable.<br />Variable must exist and be of correct provided type.
    /// </summary>
    /// <typeparam name="T">The Type of the variable.</typeparam>
    /// <param name="key">The name of the variable.</param>
    /// <param name="value">The value you which to inflict.</param>
    public void SetValue<T>(string key, T value)
    {
        if (!Vars.ContainsKey(key) || Vars[key].GetType() != typeof(T)) return;
        Vars[key] = value;
    }
    /// <summary>
    /// Gets the value of a variable.<br />Will give default value if Variable doesn't exist or is wrong type.<br />Consider using Exists<T>(key) to check if the variable exists.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns>Returns the value of the variable, or default if the variable doesn't exist.</returns>
    public T GetValue<T>(string key) => !Vars.ContainsKey(key) || Vars[key].GetType() != typeof(T) ? default : (T)Vars[key];
    /// <summary>
    /// Returns whether the Variable of type T with name key does exist.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns>Returns true if the Variable does exist and is the right type, false otherwise.</returns>
    public bool Exists<T>(string key) => Vars.ContainsKey(key) && typeof(T) == Vars[key].GetType();

}
