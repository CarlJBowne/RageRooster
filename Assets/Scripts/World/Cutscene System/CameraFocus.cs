using Unity;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class CameraFocus : MonoBehaviour
{
    public PlayableDirector cameraTimeline;

    public void OnTriggerCamera()
    {
        cameraTimeline.Play();
    }
}