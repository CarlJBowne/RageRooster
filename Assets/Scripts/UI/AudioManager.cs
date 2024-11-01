using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource musicSource;
    public AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(string musicName)
    {
        AudioClip musicClip = Resources.Load<AudioClip>(musicName);
        if (musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }
    }

    public void PlaySFX(string sfxName)
    {
        AudioClip sfxClip = Resources.Load<AudioClip>(sfxName);
        if (sfxClip != null)
        {
            sfxSource.PlayOneShot(sfxClip);
        }
    }
}