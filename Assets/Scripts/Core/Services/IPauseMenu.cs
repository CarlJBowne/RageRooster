using System;
using System.Collections.Generic;
using System.Text;

namespace RageRooster.Core
{
    public interface IPauseMenu
    {
        public bool canPause { get; set; }
        public void Pause();
    }
}
