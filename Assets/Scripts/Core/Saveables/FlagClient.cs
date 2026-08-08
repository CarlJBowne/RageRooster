using System;
using System.Collections.Generic;
using System.Text;
using SLS.SaveData;

namespace RageRooster.Core.Save
{
    [System.Serializable]
    public class FlagClient<T> where T : struct
    {
        public string ID;
        public string AreaID;
        private FlagBase.Flag<T> foundFlag;

        public bool TryGet(out T res)
        {
            res = default;
            return Find() && foundFlag.TryGetValue<T>(out res);
        }
        public bool TrySet(T value) => Find() && foundFlag.TrySetValue(value);
        public bool Find()
        {
            if (foundFlag != null) return true;





            return (foundFlag != null);
        }
        public void RegisterCallback(Action<T> input)
        {
            if (!Find()) return;
            foundFlag.OnValueChanged += input;
        }
        public void UnregisterCallback(Action<T> input)
        {
            if (!Find()) return;
            foundFlag.OnValueChanged -= input;
        }
    }
}
