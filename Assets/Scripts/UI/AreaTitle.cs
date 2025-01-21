using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaTitle : MonoBehaviour
{
    public GameObject title;

    // Start is called before the first frame update
    void Start()
    {
        Invoke("Show",1f);
    }

    void Show()
    {
        title.SetActive(true);
        Invoke("Hide",3f);
    }

    void Hide()
    {
        title.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
