using System;
using System.Collections.Generic;
using System.Text;
using SLS.GeneralUtilities.Events;
using SLS.MenuCore;
using SLS.Singletons;

namespace RageRooster.Core
{
    [UnityEngine.DefaultExecutionOrder(ExecutionOrders.UI)]
    public abstract class PauseMenu : Menu
    {
        protected static readonly Singleton<PauseMenu> S = new();
        public static PauseMenu Get => S.Get;
        public static bool TryGet(out PauseMenu instance) => S.TryGet(out instance);
        public static bool Present => S.Active;
        public static bool Paused => Get.isActive;

        public static bool CanPause
        {
            get => canPauseTicket != null && canPauseTicket.Subscribed;
            set
            {
                if (value) canPauseTicket.Subscribe();
                else canPauseTicket.UnSubscribe();
            }
        }
        protected static EventTicket canPauseTicket = null;


        public static event System.Action onPause;
        public static event System.Action onUnPause;
        public static event System.Action<bool> onPauseValue;

        public static EventTicket SubscribeOnPause(Action res, bool subscribeNow = true) => new(onPause, res, subscribeNow);
        public static EventTicket SubscribeOnUnPause(Action res, bool subscribeNow = true) => new(onUnPause, res, subscribeNow);

        protected static class DoStatic
        {
            public static void OnPause()
            {
                onPause?.Invoke();
                onPauseValue(true);
            }
            public static void OnUnPause()
            {
                onUnPause?.Invoke();
                onPauseValue(false);
            }
        }

        public static void Pause(bool value = true)
        {
            if (value) Get.Open();
            else Get.Close();
        }
    }
}