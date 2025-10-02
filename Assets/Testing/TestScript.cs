using EditorAttributes;
using FMOD.Studio;
using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RageRooster.Systems.ObjectPool;

public class TestScript : MonoBehaviour
{
    public ObjectPools.Client client;



    private void Awake()
    {
        client.Initialize();
    }

    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
            var obj = client.Pump();
            if (obj != null)
            {
                obj.transform.position = transform.position + Vector3.up * 2;
                obj.GetComponent<Rigidbody>().AddForce(Vector3.up * 5, ForceMode.Impulse);
            }
        }
    }
}
