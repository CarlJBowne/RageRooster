using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B2A_Shockwave : MonoBehaviour
{
    public ObjectPool pool;

    public void MakeShockwave() => pool.Pump();

}
