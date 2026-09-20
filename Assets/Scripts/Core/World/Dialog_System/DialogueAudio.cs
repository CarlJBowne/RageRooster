using UnityEngine;
using TMPro;
using DG.Tweening;
using FMOD;
using FMODUnity;

public class DialogueAudio : MonoBehaviour
{
    private SpeakerScript speaker;
    private TMP_Animated animatedText;

    [SerializeField] private EventReference NPC_bark;

    //leaving this here to remember implementation incase of future changes
    /*
    #region Old Audio References
        public AudioClip[] voices;
        public AudioClip[] punctuations;
        [Space]
        public AudioSource voiceSource;
        public AudioSource punctuationSource;
        public AudioSource effectSource;
    #endregion
    */

    // Start is called before the first frame update
    void Start()
    {
        speaker = GetComponent<SpeakerScript>();

        animatedText = ConversationManager.instance.animatedText;

        //animatedText.onTextReveal.AddListener((newChar) => ReproduceSound(newChar));

        //Code for running a runtime oneshot of a given sound or event.
        //Have all NPC's use the same sound event for their barks, to reduce complexity
        //RuntimeManager.PlayOneShot(NPC_bark, transform.position);
    }

    /*
    public void ReproduceSound(char c)
    {

        if (speaker != ConversationManager.instance.currentSpeaker)
            return;

        if (char.IsPunctuation(c) && !punctuationSource.isPlaying)
        {
            voiceSource.Stop();
            punctuationSource.clip = punctuations[Random.Range(0, punctuations.Length)];
            punctuationSource.Play();
        }

        if (char.IsLetter(c) && !voiceSource.isPlaying)
        {
            punctuationSource.Stop();
            voiceSource.clip = voices[Random.Range(0, voices.Length)];
            voiceSource.Play();
        }

    }
    */



}
