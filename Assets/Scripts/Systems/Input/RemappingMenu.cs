using EditorAttributes;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using Button = UnityEngine.InputSystem.InputAction;
using RebindOP = UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation;

public class RemappingMenu : MonoBehaviour, ICustomSerialized
{
    public GameObject rebindingOverlay;
    public TMPro.TextMeshProUGUI rebindingText;


    [SerializeField]
    private ButtonEntry[] buttons = new ButtonEntry[0];


    public void Rebind(int id) => buttons[id].RebindControl(this);

    public void UpdateAllIcons()
    {foreach (ButtonEntry item in buttons) item.UpdateImages();}
    public void ClearAllOverrides() 
    {foreach (var item in buttons) item.ClearOverrides();}

    public JToken Serialize()=> new JObject(
        new JProperty("Jump",       buttons[0].Serialize()),
        new        JProperty("Attack",     buttons[1].Serialize()),
        new        JProperty("Parry",      buttons[2].Serialize()),
        new        JProperty("Grab",       buttons[3].Serialize()),
        new        JProperty("Shoot",      buttons[4].Serialize()),
        new        JProperty("ShootMode",  buttons[5].Serialize()),
        new        JProperty("Charge",     buttons[6].Serialize())
            );
    public void Deserialize(JToken Data)
    {
        buttons[0].Deserialize(Data["Jump"]);
        buttons[1].Deserialize(Data["Attack"]);
        buttons[2].Deserialize(Data["Parry"]);
        buttons[3].Deserialize(Data["Grab"]);
        buttons[4].Deserialize(Data["Shoot"]);
        buttons[5].Deserialize(Data["ShootMode"]);
        buttons[6].Deserialize(Data["Charge"]);
    }

    [System.Serializable]
    public struct ButtonEntry : ICustomSerialized
    {
        public InputActionReference main;
        public string displayName;
        public InputActionReference[] relatives;
        public Image keyboardImage;
        public Image gamepadImage;
        //string activeGamepadOverride;
        //string activeKeyboardOverride;


        public ButtonEntry(InputActionReference main, params InputActionReference[] relatives)
        {
            this.main = main;
            this.relatives = relatives;
            displayName = "";
            keyboardImage = null;
            gamepadImage = null;
            //activeKeyboardOverride = "";
            //activeGamepadOverride = "";
        }

        public void RebindControl(RemappingMenu menu)
        {
            Input.Get().asset.Disable();
            menu.rebindingOverlay.SetActive(true);
            menu.rebindingText.text = $"Now Rebinding Controls for [{displayName}]";

            ButtonEntry This = this;

            main.action.PerformInteractiveRebinding()
            .WithCancelingThrough("Escape")
            .WithCancelingThrough("<Gamepad>/start")
            .WithTimeout(10f)
            .OnApplyBinding((op, path) =>
            {
                string chosenScheme = null;
                foreach (InputControlScheme scheme in Input.Get().asset.controlSchemes)
                    if (scheme.SupportsDevice(op.selectedControl.device))
                    {
                        chosenScheme = scheme.bindingGroup;
                        break;
                    }
                This.SetControl(path, chosenScheme);

            })
            .OnComplete(op =>
            {
                Input.Get().asset.Enable();
                menu.rebindingOverlay.SetActive(false);
                op.Dispose();
            })
            .Start();
        }


        private void SetControl(string path, string group)
        {
            main.action.ApplyBindingOverride(path, group: group);
            foreach (Button relative in relatives)
                relative.ApplyBindingOverride(path, group: group);

            //if (group == "Gamepad") activeGamepadOverride = path;
            //else activeKeyboardOverride = path;

            UpdateImages();
        }

        public JToken Serialize() => new JObject(
            new JProperty("Gamepad", main.action.GetBindingOverridePath(group: "Gamepad")),
            new JProperty("Keyboard", main.action.GetBindingOverridePath(group: "Keyboard"))
            ); 
        public void Deserialize(JToken Data)
        {
            ClearOverrides();
            string G = Data["Gamepad"].As<string>();
            string K = Data["Keyboard"].As<string>();
            if (!string.IsNullOrEmpty(G)) SetControl(G, "Gamepad");
            if (!string.IsNullOrEmpty(K)) SetControl(K, "Keyboard");
        }

        public void UpdateImages()
        {
            keyboardImage.sprite = ButtonIcons.Get().GetKeyboardSprite(main.action.GetBindingEffectivePath("Keyboard"));
            gamepadImage.sprite = ButtonIcons.Get().GetGamepadSprite(main.action.GetBindingEffectivePath("Gamepad"));
            keyboardImage.enabled = keyboardImage.sprite != null;
            gamepadImage.enabled = gamepadImage.sprite != null;
        }
        public void ClearOverrides()
        {
            main.action.RemoveAllBindingOverrides();
            foreach (var item in relatives) item.action.RemoveAllBindingOverrides();
        }

    }

}

public static class __RemappingExtensions
{
    public static string GetBindingPath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].path;
    public static string GetBindingEffectivePath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].effectivePath;
    public static string GetBindingOverridePath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].overridePath;
}

/*
For Future Reference.
Literally everything you could need to know about Remapping can be found in these two pages.
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.html
*/