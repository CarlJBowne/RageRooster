using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete]
public interface IPoolable_OBSOLETE
{
    public PoolableObject_OBSOLETE poolableObject { get; }

    public void OnPool();
    public void OnPump();
}
