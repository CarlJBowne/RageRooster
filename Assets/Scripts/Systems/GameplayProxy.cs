using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayProxy : MonoBehaviour
{
    public bool reverseProxy;

    private void Start()
     {
        var attempt = GameObject.Find("Gameplay");
        if(!reverseProxy)
        {
            if(attempt == null)
            {
                GameObject NEW = Instantiate(GlobalPrefabs.NamedPrefab("Gameplay"));
                NEW.name = "Gameplay";
                DontDestroyOnLoad(NEW);
                PlayerHealth Player = NEW.transform.GetChild(0).GetComponent<PlayerHealth>();
                //Player.SetRespawnPoint(transform.position);
                //Player.Respawn();

                Destroy(gameObject);
            }
            else
            {
                PlayerHealth Player = attempt.transform.GetChild(0).GetComponent<PlayerHealth>();
                //Player.SetRespawnPoint(transform.position);
                //Player.Respawn();
            }
        }
        else if(reverseProxy && attempt != null) Destroy(attempt);
    }
}
