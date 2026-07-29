using System;
using System.Collections.Generic;
using System.Text;
using SLS.GameStateMachine;
using SLS.MenuCore;


public class Boot : GameStateSingle<Boot>
{
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


    protected override void OnEnterLogic() => OnBoot();

    private void OnBoot()
    {
        var d = AudioManager.Get;
        Overlay.Instantiate();
        // Insert Boot functionality here.
    }
}