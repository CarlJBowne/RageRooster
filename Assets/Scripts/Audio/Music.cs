using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// A management classes for handling in-game music.
    /// </summary>
    public static class Music
    {
        /// <summary>
        /// A running channel of music playing.
        /// </summary>
        public class Channel
        {
            /// <summary>
            /// The FMOD event instance for this music channel.
            /// </summary>
            public EventInstance instance;
            /// <summary>
            /// The FMOD event description for this music channel.
            /// </summary>
            public EventDescription description;
            /// <summary>
            /// Whether this channel is currently playing.
            /// </summary>
            public bool playing { get; private set; }
            /// <summary>
            /// Whether this channel has been paused.
            /// </summary>
            public bool paused { get; private set; }

            /// <summary>
            /// Initializes a new <see cref="Channel"/> instance with the given FMOD event.
            /// </summary>
            /// <param name="musicEvent">The FMOD Music Event to Begin.</param>
            public Channel(EventReference musicEvent)
            {
                instance = RuntimeManager.CreateInstance(musicEvent);
                description = RuntimeManager.GetEventDescription(musicEvent);
                if (!instance.isValid() || !description.isValid()) throw new Exception("Invalid event.");
                playing = false;
            }

            /// <summary>
            /// Begin playing this music <see cref="Channel"/>.
            /// </summary>
            /// <exception cref="Exception"></exception>
            public void Begin()
            {
                if(!instance.isValid()) throw new Exception("No valid instance to play.");
                if (playing) return;
                instance.start();
                playing = true;
            }
            /// <summary>
            /// Fades out this music <see cref="Channel"/>.
            /// </summary>
            public void FadeOut()
            {
                if (!playing) return;
                instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                instance.release();
                playing = false;
            }
            /// <summary>
            /// Instantly stops this music <see cref="Channel"/>.
            /// </summary>
            public void HardStop()
            {
                if (!playing) return;
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
                playing = false;
            }

            /// <summary>
            /// Pauses the playing of this music <see cref="Channel"/>. (Unfinished, Investigate how to achieve later.)
            /// </summary>
            public void Pause()
            {
                if (!playing || paused) return;

                Enum().Begin();
                IEnumerator Enum()
                {
                    float V = 1f;
                    while (V > 0f)
                    {
                        instance.setVolume(V);
                        V -= 0.1f * Time.unscaledDeltaTime;
                        yield return null;
                    }
                    instance.setPaused(true);
                    paused = true;
                }
            }
            /// <summary>
            /// Resumes the playing of this music <see cref="Channel"/>. (Unfinished, Investigate how to achieve later.)
            /// </summary>
            public void UnPause()
            {
                if (!playing || !paused) return;

                Enum().Begin();
                IEnumerator Enum()
                {
                    float V = 0f;
                    while (V > 1f)
                    {
                        instance.setVolume(V);
                        V += 0.1f * Time.unscaledDeltaTime;
                        yield return null;
                    }
                    instance.setPaused(false);
                    paused = false;
                }
            }
        }

        /// <summary>
        /// The primary Music channel running for the current area.
        /// </summary>
        public static Channel Primary { get; private set; }
        public static Channel Secondary { get; private set; }

        /// <summary>
        /// Begin / Switch a new primary music track.
        /// </summary>
        /// <param name="newSong"></param>
        public static void BeginPrimaryMusic(EventReference newSong)
        {
            Primary?.FadeOut();
            Primary = new(newSong);
            Primary.Begin();
        }

        /// <summary>
        /// Fade out both music channels.
        /// </summary>
        public static void FadeOutBothMusic()
        {
            Primary?.FadeOut();
            Primary = null;
            Secondary?.FadeOut();
            Secondary = null;
        }

        /// <summary>
        /// Instantly stop all music.
        /// </summary>
        public static void StopAllMusic() 
        {
            Primary?.HardStop();
            Primary = null;
            Secondary?.HardStop();
            Secondary = null;
        }

        public static void BeginSecondaryMusic(EventReference input)
        {
            Routine().Begin();
            IEnumerator Routine()
            {
                bool existingSameSecondary = Secondary != null && Secondary.description == input.description;

                if (!existingSameSecondary)
                {
                    float F = 1f;
                    Secondary = new(input);
                    Secondary.Begin();
                    while (F > 0f)
                    {
                        Primary.instance.setVolume(F);
                        F -= Time.unscaledDeltaTime;
                        yield return null;
                    }
                    Primary.instance.setVolume(0f);
                    Primary.instance.setPaused(true);
                }
                else
                {
                    Secondary.instance.setPaused(false);
                    float F = 1f;
                    while (F > 0f)
                    {
                        Primary.instance.setVolume(F);
                        Secondary.instance.setVolume(1 - F);
                        F -= Time.unscaledDeltaTime;
                        yield return null;
                    }
                    Primary.instance.setVolume(0f);
                    Primary.instance.setPaused(true);
                    Secondary.instance.setVolume(1f);
                }
            }
        }

        public static void ReturnToPrimaryMusic()
        {
            Routine().Begin();
            IEnumerator Routine()
            {
                if (Secondary == null) yield break;
                float F = 0f;
                Primary.instance.setPaused(false);
                while (F < 1f)
                {
                    Primary.instance.setVolume(F);
                    Secondary.instance.setVolume(1 - F);
                    F += Time.unscaledDeltaTime;
                    yield return null;
                }
                Primary.instance.setVolume(1f);
                Secondary.instance.setVolume(0f);
                Secondary.instance.setPaused(true);
            }
        }





    }

}