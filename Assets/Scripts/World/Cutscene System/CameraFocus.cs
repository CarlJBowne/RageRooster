using Unity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[RequireComponent(typeof(PlayableDirector))]
public class CameraFocus : MonoBehaviour
{
    public PlayableDirector cameraTimeline;

    public void OnTriggerCamera()
    {
        cameraTimeline.Play();
    }
}