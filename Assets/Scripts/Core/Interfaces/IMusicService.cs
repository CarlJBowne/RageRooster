using System;
using System.Runtime.CompilerServices;
using FMODUnity;

[assembly: InternalsVisibleTo("RageRooster.Audio")]

namespace RageRooster.Core
{
    public interface IMusicService
    {
        public static IMusicService Self => RageRooster.Services.Music;
        public static bool Present => Self != null;

        void StopAllMusic();
        void BeginPrimaryMusic(EventReference newSong);
        void FadeOutBothMusic();
        void BeginSecondaryMusic(EventReference input);
        void ReturnToPrimaryMusic();
    }
}
