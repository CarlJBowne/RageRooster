using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Button = UnityEngine.InputSystem.InputAction;

public class RemappingMenu : MonoBehaviour
{
    public GameObject rebindingOverlay;
    public TMPro.TextMeshProUGUI rebindingText;


    public void RebindAttack() => RebindControl(Input.Attack, "Attack");

    public void RebindControl(Button button, string name, params Button[] relatives)
    {
        Input.Get().asset.Disable();
        rebindingOverlay.SetActive(true);
        rebindingText.text = $"Now Rebinding Controls for [{name}]";

        button.PerformInteractiveRebinding()
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
            button.ApplyBindingOverride(path, group: chosenScheme);
            foreach (Button relative in relatives)
                relative.ApplyBindingOverride(path, group: chosenScheme);
        })
        .OnComplete(op =>
        {
                Input.Get().asset.Enable();
                rebindingOverlay.SetActive(false);
                op.Dispose();
        })
        .Start();
    }




}

/*
For Future Reference.
Literally everything you could need to know about Remapping can be found in these two pages.
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.html
*/