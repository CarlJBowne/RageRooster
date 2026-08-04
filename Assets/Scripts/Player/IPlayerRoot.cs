using RageRooster.Core;
using UnityEngine;

namespace RageRooster.Player
{
    public interface IPlayerRoot : IPlayer
    {
        PlayerMovementBody MovementBody { get; }
        PlayerController Controller { get; }
        PlayerRanged Ranged { get; }
        PlayerGrabber Grabber { get; }
        Animator Animator { get; }
        AudioCaller Audio { get; }
        RagdollHandler RagdollHandler { get; }
        TargetingManager TargetingManager { get; }
        SLS.StateMachineH.Signals.SignalManager SignalManager { get; }
    }
}
