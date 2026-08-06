using System;
using System.Collections.Generic;
using System.Text;

namespace RageRooster.Core
{
    public interface ISaveData
    {
        public static ISaveData Active => RageRooster.Services.SaveSystem.Active;
        public static bool Present => Active != null;

    }
}
