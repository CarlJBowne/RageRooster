using System.Collections;
using System.Collections.Generic;
using RageRooster.Systems.ObjectPooling;
using UnityEngine;

public class EnemyBasicBulletSource : MonoBehaviour
{
    public Transform muzzle;
    bool initialized = false;

    //private void Awake() => Initialize();

    public void Initialize()
    {
        if (initialized) return;
        GlobalPool.BasicEnemyBullet.Initialize();
        initialized = true;
    }

    public PoolableObject Pump(bool autoEnable = true)
    {
        if (!initialized) Initialize();
        PoolableObject res = null;
        GlobalPool.BasicEnemyBullet.Pump(Success);

        void Success(PoolableObject obj, AttackProjectile proj)
        {
            if (muzzle != null) obj.PlaceAtMuzzle(muzzle);
            //proj.Send();
            obj.currentClient = this;
            if (autoEnable) obj.Active = true;
            res = obj;
        }
        return res;
    }
}
