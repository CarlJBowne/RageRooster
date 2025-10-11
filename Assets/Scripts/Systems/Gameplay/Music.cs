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

        public static StudioEventEmitter Emitter;
        public static StudioEventEmitter SecondaryEmitter;

        public static void PlayMusic(EventReference @event) => Emitter.CrossFadeMusic(@event);

        public static void BeginSecondaryMusic(EventReference @event) 
        {
            Emitter.Stop();
            SecondaryEmitter.ChangeEvent(@event);
            SecondaryEmitter.Play();
        }

        public static void StopAllMusic() 
        {
            Emitter?.Stop();
            SecondaryEmitter?.Stop();
        }
    }
}