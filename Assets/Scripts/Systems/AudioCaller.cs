using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using AYellowpaper.SerializedCollections;

public class AudioCaller : MonoBehaviour
{
    public SerializedDictionary<string, EventReference> events;
    public EventReference this[string id] => events[id];

    public void PlayOneShot(string id) => FMODUnity.RuntimeManager.PlayOneShot(events[id]);
}

public static class _FMODExtensions
{
    public static void PlayOneShot(this EventReference THIS, Vector3 pos = default) => FMODUnity.RuntimeManager.PlayOneShot(THIS, pos);
}