using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpin : MonoBehaviour
{

 void Update()
    {
        transform.Rotate(0f, 0f, 100f * Time.deltaTime, Space.Self);
    }
}
