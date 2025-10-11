using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageRooster.Systems
{
    public static class Music
    {

        public class Channel
        {
            public EventInstance instance;
            public EventDescription description;
            public bool playing { get; private set; }
            public bool paused { get; private set; }

            public Channel(EventReference musicEvent)
            {
                instance = RuntimeManager.CreateInstance(musicEvent);
                description = RuntimeManager.GetEventDescription(musicEvent);
                if (!instance.isValid() || !description.isValid()) throw new Exception("Invalid event.");
                playing = false;
            }

            public void Begin()
            {
                if(!instance.isValid()) throw new Exception("No valid instance to play.");
                if (playing) return;
                instance.start();
                playing = true;
            }
            public void FadeOut()
            {
                if (!playing) return;
                instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                instance.release();
                playing = false;
            }
            public void HardStop()
            {
                if (!playing) return;
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
                playing = false;
            }

            public void Pause()
            {
                if (!playing) return;
                if(paused) return;
                instance.setPaused(true);
                paused = true;
            }
            public void UnPause()
            {
                if (!playing) return;
                if (!paused) return;
                instance.setPaused(false);
                paused = false;
            }
        }

        public static Channel Primary { get; private set; }
        //public static Channel Secondary { get; private set; }


        public static void BeginPrimaryMusic(EventReference newSong)
        {
            Primary?.FadeOut();
            Primary = new(newSong);
            Primary.Begin();
        }

        public static void FadeOutBothMusic()
        {
            Primary?.FadeOut();
            Primary = null;
            //Secondary?.FadeOut();
            //Secondary = null;
        }

        public static void StopAllMusic() 
        {
            Primary?.HardStop();
            Primary = null;
            //Secondary?.HardStop();
            //Secondary = null;
        }
    }

}