using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RageRooster.Core.World;
using RageRooster.Settings;
using SLS.AppStateMachine;
using SLS.MenuCore;


public class Boot : AppStateGlobal<Boot>
{
    public AppState MainMenuState;
    public AppState GameplayState;

    private int loadFromSavePointID = -2;
    public static int LoadFromSavePointID
    {
        get => Get.loadFromSavePointID;
        set => Get.loadFromSavePointID = value;
    }

    public enum OnBuildStateMachineHandling
    {
        DoNothing,
        SetupIfNotSetup,
        SetupIfNotSetupAndSave,
        SetupRegardless,
        SetupRegardlessAndSave
    }
    public OnBuildStateMachineHandling onBuildStateMachineHandling;

    protected override IEnumerator OnEnter()
    {
        base.OnEnter();
        OnBoot();
        yield break;
    }

    private void OnBoot()
    {
        GameSettings.Init();
        var d = AudioManager.Get;
        Overlay.Instantiate();
    }

}