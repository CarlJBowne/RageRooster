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
    {foreach (ButtonEntry item in buttons) item.ClearOverrides();}

    public JToken Serialize(string name = null) => new JProperty(name, new JObject(
        buttons[0].Serialize(),
        buttons[1].Serialize(),
        buttons[2].Serialize(),
        buttons[3].Serialize(),
        buttons[4].Serialize(),
        buttons[5].Serialize(),
        buttons[6].Serialize(),
        buttons[7].Serialize()
            ));
    public static implicit operator JToken(RemappingMenu THIS) => THIS.Serialize();
    public void Deserialize(JToken Data)
    {
        buttons[0].Deserialize(Data[buttons[0].displayName]);
        buttons[1].Deserialize(Data[buttons[1].displayName]);
        buttons[2].Deserialize(Data[buttons[2].displayName]);
        buttons[3].Deserialize(Data[buttons[3].displayName]);
        buttons[4].Deserialize(Data[buttons[4].displayName]);
        buttons[5].Deserialize(Data[buttons[5].displayName]);
        buttons[6].Deserialize(Data[buttons[6].displayName]);
        buttons[7].Deserialize(Data[buttons[7].displayName]);
    }

    /// <summary>
    /// Rebinding appears to not work on the Input class because it creates a new instance and this system makes changes to the base asset. This Method fixes that on startup.
    /// </summary>
    public void TargetInput()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].main = Input.Get().Asset.FindAction(buttons[i].main.action.name).Reference();
            for (int j = 0; j < buttons[i].relatives.Length; j++)
            {
                buttons[i].relatives[j] = Input.Get().Asset.FindAction(buttons[i].relatives[j].action.name).Reference();
            }
        }
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
            Input.Get().Asset.Disable();
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
                foreach (InputControlScheme scheme in Input.Get().Asset.controlSchemes)
                    if (scheme.SupportsDevice(op.selectedControl.device))
                    {
                        chosenScheme = scheme.bindingGroup;
                        break;
                    }
                This.SetControl(path, chosenScheme);

            })
            .OnComplete(op =>
            {
                Input.Get().Asset.Enable();
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

        public JToken Serialize(string name = null) => new JProperty(displayName, new JObject(
                new JProperty("Gamepad", main.action.GetBindingOverridePath(group: "Gamepad")),
                new JProperty("Keyboard", main.action.GetBindingOverridePath(group: "Keyboard"))
                ));
        public static implicit operator JToken(ButtonEntry THIS) => THIS.Serialize(THIS.displayName);

        public void Deserialize(JToken Data)
        {
            if (Data == null) return;
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

    public static string GetControlString(string input)
    {
        RemappingMenu R = SettingsMenu.Get().remap;

        int i = 0;
        for (; i < R.buttons.Length; i++) 
            if (R.buttons[i].displayName == input)
                break;
        if (i == R.buttons.Length) return null;
        string stringG = R.buttons[i].main.action.GetBindingDisplayString(options: InputBinding.DisplayStringOptions.DontIncludeInteractions, group: "Gamepad");
        string stringK = R.buttons[i].main.action.GetBindingDisplayString(options: InputBinding.DisplayStringOptions.DontIncludeInteractions, group: "Keyboard");

        return $"{stringG} / {stringK}";
    }


}

public static class __RemappingExtensions
{
    public static string GetBindingPath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].path;
    public static string GetBindingEffectivePath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].effectivePath;
    public static string GetBindingOverridePath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].overridePath;

    public static InputActionReference Reference(this InputAction This) => InputActionReference.Create(This);
}

/*
For Future Reference.
Literally everything you could need to know about Remapping can be found in these two pages.
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.html
*/