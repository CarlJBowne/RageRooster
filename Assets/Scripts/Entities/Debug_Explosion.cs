using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_Explosion : MonoBehaviour
{
    public Timer.OneTime timer = new(1f);

    public void Start()
    {
        timer.Begin();
    }
    public void Update()
    {
        timer.Tick(RemoveExplosion);
    }

    public void RemoveExplosion()
    {
        Destroy(gameObject);
    }
}
