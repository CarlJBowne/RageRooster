using RageRooster.Core;
using UnityEngine;

namespace RageRooster.Player
{
    public static class Self
    {
        public static IPlayerRoot Instance { get; internal set; }
        
        public static bool Present => Instance != null;
        
        public static PlayerMovementBody MovementBody => Instance?.MovementBody;
        public static PlayerController Controller => Instance?.Controller;
        public static PlayerRanged Ranged => Instance?.Ranged;
        public static PlayerGrabber Grabber => Instance?.Grabber;
        public static Animator Animator => Instance?.Animator;
        public static AudioCaller Audio => Instance?.Audio;
        public static PlayerStateMachine StateMachine => Instance?.StateMachine as PlayerStateMachine;
        public static CapsuleCollider Collider => Instance?.Collider;
        public static Transform Transform => Instance?.Transform;
        public static GameObject GameObject => Instance?.GameObject;

        public static PlayerRoot.HealthModel Health => (Instance as PlayerRoot)?.health;
        public static PlayerRoot.AmmoModel Ammo => (Instance as PlayerRoot)?.ammo;
        public static PlayerRoot.CurrencyModel Currency => (Instance as PlayerRoot)?.currency;

        public static void Death() => Instance?.Death();
        public static void PitFall() => Instance?.PitFall();
    }
}
