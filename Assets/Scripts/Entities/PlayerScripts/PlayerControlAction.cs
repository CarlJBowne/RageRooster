using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControlAction : PlayerStateBehavior
{
    public string animationName;

    public SerializedDictionary<InputAction, PlayerControlAction> feedingActions;

    public Upgrade necessaryUpgrade;

    public override void OnAwake() => controller.ApplyCurrentAction(this);

}