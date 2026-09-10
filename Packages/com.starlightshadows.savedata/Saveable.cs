using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SLS.SaveData
{
    [System.Serializable]
    public abstract class Saveable<T> : SaveableGeneric where T : Saveable<T>
    {
        public abstract void Clone(T source);
        public static void Clone(T from, T to) => to.Clone(from);
    }
    [System.Serializable]
    public abstract class SaveableGeneric
    {
        public abstract void Clone(SaveableGeneric source);
        public static void Clone(SaveableGeneric from, SaveableGeneric to) => to.Clone(from);
    }
}
