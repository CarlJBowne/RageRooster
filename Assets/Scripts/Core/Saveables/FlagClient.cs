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
        private Flag.Generic<T> foundFlag;

        public bool TryGet(out T res)
        {
            res = default;
            if (Find())
            {
                res = foundFlag.Value;
                return true;
            }
            return false;
        }
        public bool TrySet(T value)
        {
            if (!Find()) return false;
            foundFlag.Value = value;
            return true;
        }
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
