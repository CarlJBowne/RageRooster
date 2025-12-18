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
    [SerializeReference]
    public PolymorphExamplebase polymorphicObject;
    //[SerializeReference]
    //public PolymorphicList<PolymorphExamplebase> polymorphicList;
}


[System.Serializable]
public abstract class PolymorphExamplebase : PolymorphicObject
{
    public PolymorphExamplebase() { }
}

[System.Serializable]
public class PolymorphExampleInt : PolymorphExamplebase
{
    public int value = 1;
}
[System.Serializable]
public class PolymorphExampleString : PolymorphExamplebase
{
    public string value = "Hello";
}
[System.Serializable]
public class PolymorphExampleFloat : PolymorphExamplebase
{
    public float value = 1.0f;
}