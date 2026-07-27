using System.Collections;
using System.Collections.Generic;
using RageRooster.Systems.SaveSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DebugToolWindow : EditorWindow
{
    [MenuItem("Rage Rooster Tooling/Show Debug Window", priority = -1)]
    public static new void Show()
    {
        DebugToolWindow w = ScriptableObject.CreateInstance<DebugToolWindow>();
        w.titleContent = new("Debug Tools Window");
        w.ShowUtility();
    }

    Label onlyInGameMessage;
    VisualElement actualWindowRoot;
    Foldout UpgradesFoldout;


    //Open Window, if game is not playing simply show "Only use when game is playing" label.
    private void OnEnable()
    {
        onlyInGameMessage = new Label("This Window is meant only to display in game. Please begin the game.");
        actualWindowRoot = new();
        rootVisualElement.Add(onlyInGameMessage);
        rootVisualElement.Add(actualWindowRoot);

        if (Gameplay.Active)
        {
            onlyInGameMessage.SetEnabled(false);
            BeginWindow();
        }
        else actualWindowRoot.SetEnabled(false);

        Gameplay.onFinalAwake += BeginWindow;
        Gameplay.onDestroy += EndWindow;

    }
    private void OnDisable()
    {
        Gameplay.onFinalAwake -= BeginWindow;
        Gameplay.onDestroy -= EndWindow;
    }

    //Intializes the window for real when the game is playing.
    void BeginWindow()
    {
        onlyInGameMessage.SetEnabled(false);
        actualWindowRoot.SetEnabled(true);
        actualWindowRoot.Clear();

        UpgradesFoldout = new()
        {
            text = "Active Upgrades"
        };
        actualWindowRoot.Add(UpgradesFoldout);

        CreateUpgradeDisplay(Upgrades.Upgrade.DropLaunch);
        CreateUpgradeDisplay(Upgrades.Upgrade.WallJump);
        CreateUpgradeDisplay(Upgrades.Upgrade.Hellcopter);
        CreateUpgradeDisplay(Upgrades.Upgrade.RagingCharge);
        CreateUpgradeDisplay(Upgrades.Upgrade.Glide);
        CreateUpgradeDisplay(Upgrades.Upgrade.DoubleJump);
        CreateUpgradeDisplay(Upgrades.Upgrade.Lasso);

        {
            Toggle moonJumpToggle = new()
            {
                text = "Moon Jump",
                value = Upgrades.Active.d_moonJump
            };
            moonJumpToggle.RegisterValueChangedCallback(ValueChanged);
            void ValueChanged(ChangeEvent<bool> value) => Upgrades.Active.d_moonJump = value.newValue;

            UpgradesFoldout.Add(moonJumpToggle);
        }
        {
            Toggle invinceToggle = new()
            {
                text = "Invincibility",
                value = Upgrades.Active.d_invincibility
            };
            invinceToggle.RegisterValueChangedCallback(ValueChanged);
            void ValueChanged(ChangeEvent<bool> value) => Upgrades.Active.d_invincibility = value.newValue;

            UpgradesFoldout.Add(invinceToggle);
        }

        Toggle CreateUpgradeDisplay(Upgrades.Upgrade upgrade)
        {
            Toggle result = new()
            {
                text = upgrade.ToString(),
                value = Upgrades.Active.HasUpgrade(upgrade)
            };
            result.RegisterValueChangedCallback(ValueChanged);
            void ValueChanged(ChangeEvent<bool> value) => Upgrades.Active.SetUpgrade(upgrade, value.newValue);

            UpgradesFoldout.Add(result);
            return result;
        }




    }
    void EndWindow()
    {
        onlyInGameMessage.SetEnabled(true);
        actualWindowRoot.SetEnabled(false);
    }
}
