using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyMeOnLoad : MonoBehaviour
{
    private void Awake() => DontDestroyOnLoad(this);
}
