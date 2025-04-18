using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayerUtility : MonoBehaviour
{
    public EventReference musicToPlay;

    public void PlayMusic() => Gameplay.musicEmitter.CrossFadeMusic(musicToPlay);


}
