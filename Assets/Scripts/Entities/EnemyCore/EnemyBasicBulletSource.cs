using System.Collections;
using System.Collections.Generic;
using Utilities.ObjectPooling;
using UnityEngine;

public class EnemyBasicBulletSource : MonoBehaviour
{
    public Transform muzzle;
    bool initialized = false;

    private void Awake()
    {
        if (!Gameplay.Active) return;
        GlobalPool.BasicEnemyBullet.Initialize();
    }

    public Spawnable Pump(bool autoEnable = true)
    {
        if (!Gameplay.Active) return null;
        if (!GlobalPool.BasicEnemyBullet.initialized) GlobalPool.BasicEnemyBullet.Initialize();
        Spawnable res = null;
        GlobalPool.BasicEnemyBullet.Pump(Success);

        void Success(Spawnable obj, AttackProjectile proj)
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
