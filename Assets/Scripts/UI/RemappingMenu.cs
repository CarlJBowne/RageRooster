using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using RageRooster.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utilities.Xtensions.Input;
using Button = UnityEngine.InputSystem.InputAction;

[DefaultExecutionOrder(ExecutionOrders.GameplaySystems)]
public class RemappingMenu : MonoBehaviour
{
    public GameObject rebindingOverlay;
    public TMPro.TextMeshProUGUI rebindingText;


    [SerializeField]
    private ButtonEntry[] buttons = new ButtonEntry[0];

    public void Rebind(int i)
    {
        Enum().Begin(this); //This is unironically the stupiest thing Unity has ever forced me to do.
        IEnumerator Enum()
        {
            Input.Disable();
            Input.Jump.Disable(); //How on earth is Jump SPECFICIALLY enabled here????
            rebindingOverlay.SetActive(true);
            rebindingText.text = $"Now Rebinding Gamepad Controls for [{buttons[i].displayName}]";
            yield return null;

            GameSettings.Remapping.Remap(buttons[i].main.action, completed =>
            {
                rebindingOverlay.SetActive(false);
                if (completed) buttons[i].UpdateImages();
                Input.Enable();
                Input.Jump.Enable();
            });
        }
    }

    public void DefaultControls()
    {
        GameSettings.Remapping.ClearAllBindingOverrides();
        UpdateAllIcons();
    }

    public void UpdateAllIcons()
    { foreach (ButtonEntry item in buttons) item.UpdateImages(); }

    [System.Serializable]
    public struct ButtonEntry
    {
        public InputActionReference main;
        public string displayName;
        public Image keyboardImage;
        public Image gamepadImage;

        public void UpdateImages()
        {
            keyboardImage.sprite = ButtonIcons.Get.GetKeyboardSprite(main.action.GetBindingEffectivePath("Keyboard"));
            gamepadImage.sprite = ButtonIcons.Get.GetGamepadSprite(main.action.GetBindingEffectivePath("Gamepad"));
            keyboardImage.enabled = keyboardImage.sprite != null;
            gamepadImage.enabled = gamepadImage.sprite != null;
        }
    }

    public static string GetControlString(string input)
    {
        RemappingMenu R = SettingsMenu.Get.remap;

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

/*
For Future Reference.
Literally everything you could need to know about Remapping can be found in these two pages.
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.html
*/