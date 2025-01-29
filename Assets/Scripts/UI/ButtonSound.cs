using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour
{
    EventReference sound;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }
    public void PlaySound() => AudioManager.Get().PlayOneShot(!sound.IsNull ? sound : FMODEvents.Get().selectionConfirm, this.transform.position);
}
