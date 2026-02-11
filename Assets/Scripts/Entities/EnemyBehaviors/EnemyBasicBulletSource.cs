using System.Collections;
using System.Collections.Generic;
using RageRooster.Systems.ObjectPooling;
using UnityEngine;

public class EnemyBasicBulletSource : MonoBehaviour
{
    public Transform muzzle;
    bool initialized = false;

    public void Initialize()
    {
        if (initialized) return;
        GlobalPool.BasicEnemyBullet.Initialize();
        initialized = true;
    }

    public PoolableObject Pump(bool autoEnable = true)
    {
        if (!initialized) Initialize();
        var res = GlobalPool.BasicEnemyBullet.PumpBase();
        if (muzzle != null) res.PlaceAtMuzzle(muzzle);
        res.currentClient = this;
        if (autoEnable) res.Active = true;
        return res;
    }
}
