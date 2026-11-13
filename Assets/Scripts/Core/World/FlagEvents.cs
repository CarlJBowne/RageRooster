using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using RageRooster.Core.Save;
using SLS.GeneralUtilities.Events;
using UnityEngine;

namespace RageRooster.Core.World
{
    public class FlagEvents : MonoBehaviour
    {
        public Polymorph.ListOf<InstanceBase> events = new();

        public void Awake(){for (int i = 0; i < events.Count; i++) events[i].Init();}
        public void OnDestroy(){for (int i = 0; i < events.Count; i++) events[i].Deinit();}

        [System.Serializable]
        public abstract class InstanceBase : Polymorph
        {
            public abstract void Init();
            public abstract void Deinit();

            [System.Serializable]
            public abstract class Generic<T> : InstanceBase
            {
                public FlagClient<T> client;
                public UltEvents.UltEvent<T> onChange;
                public bool runEventOnInit = false;
                protected EventTicket ticket;

                public override void Init()
                {
                    if (!client.TryGet(out T res)) return;
                    ticket = client.RegisterCallback(onChange.Invoke);
                    if (runEventOnInit) onChange.Invoke(res);
                }
                public override void Deinit() => ticket?.UnSubscribe();
            }
            [System.Serializable] public class Bool : Generic<bool>
            {
                public UltEvents.UltEvent onTrue;

                public override void Init()
                {
                    if (!client.TryGet(out bool res)) return;
                    ticket = client.RegisterCallback(Execute);
                    if (runEventOnInit) onChange.Invoke(res);
                    if (res) onTrue.Invoke();
                }
                public override void Deinit() => ticket?.UnSubscribe();
                void Execute(bool value)
                {
                    onChange.Invoke(value);
                    if (value) onTrue.Invoke();
                }
            }
            [System.Serializable] public class Int : Generic<int> { }
            [System.Serializable] public class Float : Generic<float> { }
            [System.Serializable] public class String : Generic<string> { }
            [System.Serializable] public class Char : Generic<char> { }
            [System.Serializable] public class Vector : Generic<Vector3> { }
        }

    }
}
