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
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

public class TestScript : MonoBehaviour
{
    [PolymorphicObject.List(typeof(PlayerButtonAction)), SerializeReference]
    public List<PlayerButtonAction> buttons = new();

    /*
#if UNITY_EDITOR
    [CustomEditor(typeof(TestScript))]
    public class _Editor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return new SuperList<PlayerButtonAction>(serializedObject.FindProperty("buttons"))
            {
                preAddCallback = (list) =>
                {
                    PlayerButtonAction.ShowChooseTypeMenu(typeof(PlayerButtonAction), false, (type) =>
                    {
                        list.CreatePropertySlot(out int newID);
                        list.SetOrCreateItemValue(newID, Activator.CreateInstance(type));
                        list.CreateItemElement(newID);
                    });
                },
            };
        }
    }
#endif*/
}