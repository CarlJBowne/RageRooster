using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXCatalogue : MonoBehaviour
{
    /// <summary>
    /// The Dictionary of VFX available.
    /// </summary>
    public SerializedDictionary<string, ObjectPool> Pools;

    /// <summary>
    /// Direct access to this catalogue's ObjectPools via a name.
    /// </summary>
    /// <param name="ID"></param>
    /// <returns></returns>
    public ObjectPool this[string ID] => Pools[ID];

    /// <summary>
    /// "Pump" an instance of the desired VFX from the Object Pool, using a name to identify the desired VFX.
    /// </summary>
    /// <param name="name">The ID of the VFX. Must be EXACT.</param>
    /// <returns>Returns the PoolableObject of the VFX instance if successful. Use for further logic.</returns>
    public PoolableObject Pump(string name) => Pools.ContainsKey(name) ? Pools[name].Pump() : null;

    /// <summary>
    /// "Pump" an instance of the desired VFX from the Object Pool, using a name to identify the desired VFX. (Includes Transform Override)
    /// </summary>
    /// <param name="name">The ID of the VFX. Must be EXACT.</param>
    /// <param name="at">The Transform you'd like to place the VFX at.</param>
    /// <returns></returns>
    public PoolableObject Pump(string name, Transform at)
    {
        if(!Pools.ContainsKey(name)) return null;
        PoolableObject result = Pools[name].Pump();
        if (result && at != null)
        {
            result.SetPosition(at.position);
            result.SetRotation(at.position);
        } 
        return result;
    }

    /// <summary>
    /// "Pump" an instance of the desired VFX from the Object Pool, using a name to identify the desired VFX. (Includes Position and Rotation Override)
    /// </summary>
    /// <param name="name">The ID of the VFX. Must be EXACT.</param>
    /// <param name="position">The position you'd like to place the VFX at.</param>
    /// <param name="rotation">The rotation you'd like to place the VFX at.</param>
    /// <returns></returns>
    public PoolableObject Pump(string name, Vector3 position, Vector3 rotation = default)
    {
        if (!Pools.ContainsKey(name)) return null;
        PoolableObject result = Pools[name].Pump();
        if (result)
        {
            result.SetPosition(position);
            result.SetRotation(rotation);
        }
        return result;
    }




    private void Update()
    {
        foreach (KeyValuePair<string, ObjectPool> item in Pools)
            item.Value.Update();
    }
}
