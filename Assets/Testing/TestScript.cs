using EditorAttributes;
using FMOD.Studio;
using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RageRooster.Systems.ObjectPool;
using FMODUnity;
using RageRooster.Systems;

public class TestScript : MonoBehaviour
{
    [RelatedComponent] public Rigidbody noParams;
    [RelatedComponent(true)] public Rigidbody required;
    [RelatedComponent(subLocation = "sub/subsub")] public Rigidbody subLocation;
    [RelatedComponent(true, "sub/subsub")] public Rigidbody both;
}
