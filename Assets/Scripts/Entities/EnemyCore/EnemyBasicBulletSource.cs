using System.Collections;
using System.Collections.Generic;
using SLS.ObjectUtilities;
using UnityEngine;
using Utilities;

public class EnemyBasicBulletSource : MonoBehaviour
{
    public Transform muzzle;
    bool initialized = false;

    private void Awake()
    {
        if (!Gameplay.Active) return;
        GlobalPool.BasicEnemyBullet.Initialize();
    }

    public Spawnable Pump()
    {
        if (!Gameplay.Active) return null;
        if (!GlobalPool.BasicEnemyBullet.initialized) GlobalPool.BasicEnemyBullet.Initialize();
        Spawnable res = null;
        GlobalPool.BasicEnemyBullet.Pump(Success, muzzle);

        void Success(Spawnable obj)
        {
            if (muzzle != null) obj.transform.CopyFrom(muzzle);
            obj.currentClient = this;
            res = obj;
        }
        return res;
    }
}
