using System;
using System.Collections.Generic;
using System.Text;
using SLS.GameStateMachine;
using SLS.MenuCore;


public class Boot : GameStateSingle<Boot>
{
    protected override void OnEnterLogic() => OnBoot();

    private void OnBoot()
    {
        Overlay.Instantiate();
        // Insert Boot functionality here.
    }
}