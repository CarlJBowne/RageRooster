using AYellowpaper.SerializedCollections;
using SLS.ListUtilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using SLS.ObjectUtilities;

public class VFXCatalogue : MonoBehaviour
{
    /// <summary>
    /// The Dictionary of VFX available.
    /// </summary>
    public HashedListS<ObjectPool> Pools;

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
    public Spawnable Pump(string name) => Pools.TryGet(name, out ObjectPool found) ? found.Pump(transform) : null;

    /// <summary>
    /// "Pump" an instance of the desired VFX from the Object Pool, using a name to identify the desired VFX. (Includes Transform Override)
    /// </summary>
    /// <param name="name">The ID of the VFX. Must be EXACT.</param>
    /// <param name="at">The Transform you'd like to place the VFX at.</param>
    /// <returns></returns>
    public Spawnable Pump(string name, Transform at)
    {
        if (!Pools.TryGet(name, out ObjectPool found)) return null;
        Spawnable result = found.Pump(at);
        return result;
    }

    /// <summary>
    /// "Pump" an instance of the desired VFX from the Object Pool, using a name to identify the desired VFX. (Includes Position and Rotation Override)
    /// </summary>
    /// <param name="name">The ID of the VFX. Must be EXACT.</param>
    /// <param name="position">The position you'd like to place the VFX at.</param>
    /// <param name="rotation">The rotation you'd like to place the VFX at.</param>
    /// <returns></returns>
    public Spawnable Pump(string name, Vector3 position, Vector3 rotation = default)
    {
        if (!Pools.TryGet(name, out ObjectPool found)) return null;
        Spawnable result = found.Pump((position, rotation));
        return result;
    }




    private void Update()
    {
        for (int i = 0; i < Pools.Count; i++)
            Pools.ValueFromIndex(i).Update(Time.deltaTime);
    }
}
