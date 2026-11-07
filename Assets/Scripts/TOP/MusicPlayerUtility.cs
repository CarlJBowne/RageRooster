using Audio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayerUtility : MonoBehaviour
{
    public EventReference musicToPlay;

    public void PlayMusic() => Music.BeginPrimaryMusic(musicToPlay);


}
