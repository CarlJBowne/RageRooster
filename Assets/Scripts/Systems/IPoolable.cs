using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    public PoolableObject poolableObject { get; }

    public void OnPool();
    public void OnPump();
}
