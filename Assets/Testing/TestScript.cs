using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    [Button]
    public void QuickReset()
    {
        coll.enabled = false;
        coll.enabled = true;
    }

    public Collider coll;


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerStateMachine>(out _)) Debug.Log("YES");
    }

}
