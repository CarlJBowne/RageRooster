using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayProxy : MonoBehaviour
{
    public bool reverseProxy;

    private void Start()
    {
        var attempt = GameObject.Find("Gameplay");
        if(!reverseProxy && attempt == null)
        {
            var NEW = Instantiate(GlobalPrefabs.NamedPrefab("Gameplay"));
            NEW.transform.position = transform.position;
            NEW.name = "Gameplay";
            Destroy(gameObject);
        }
        else if(reverseProxy && attempt != null) Destroy(attempt);
    }
}
