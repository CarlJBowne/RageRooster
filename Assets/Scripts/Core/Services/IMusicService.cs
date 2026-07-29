using System;
using FMODUnity;

namespace RageRooster.Core
{
    public interface IMusicService
    {
        public static IMusicService Music => Services.Music;

        void StopAllMusic();
        void BeginPrimaryMusic(EventReference newSong);
        void FadeOutBothMusic();
        void BeginSecondaryMusic(EventReference input);
        void ReturnToPrimaryMusic();
    }
}
