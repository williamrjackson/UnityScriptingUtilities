using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Wrj
{
    /// <summary> 
    /// Play audio clips without managing AudioSources and without the clicks and pops of interruptions.
    /// </summary>
    public class AudioPool : MonoBehaviour
    {
        public AudioMixerGroup audioMixerGroup;

        private int poolCount = 1;
        private List<AudioSource> sources = new List<AudioSource>();

        void Start()
        {
            for (int i = 0; i < poolCount; i++)
            {
                CreatePoolAux();
            }
        }

        // Play oneshot.
        public void PlayOneShot(AudioClip clip)
        {
            PlayPitchOneShot(clip, 1f);
        }

        // Play oneshot. Allows setting a pitch, as well as prevents interrupted playback.
        public void PlayPitchOneShot(AudioClip clip, float pitch, float volume = 1f)
        {
            // print("PitchOneShot at pitch: " + pitch);
            // Get next available audio source
            AudioSource source = FirstNotPlaying();
            // Set it's pitch
            source.pitch = pitch;
            // Set it's volume
            source.volume = volume;
            // Play oneshot
            source.PlayOneShot(clip);
        }

        // This should allow for more "musical" pitch adjustments. Up/Down 1 octave.
        public void PlayPitchOneShot(AudioClip clip, int pitch, float volume = 1f)
        {
            float fAppliedPitchScale = 1;
            if (pitch > 0)
            {
                fAppliedPitchScale = Utils.Remap((float)pitch, 0, 12f, 1f, 2f);
            }
            else
            {
                fAppliedPitchScale = Utils.Remap((float)pitch, 0, -12f, 1f, .5f);
            }
            PlayPitchOneShot(clip, fAppliedPitchScale, volume);
        }

        private AudioSource FirstNotPlaying()
        {
            foreach (AudioSource source in sources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }
            return CreatePoolAux();
        }

        private AudioSource CreatePoolAux()
        {
            // Creat object
            GameObject newGO = new GameObject();
            // Name it
            newGO.name = "AudioPoolObjectAux";
            // Child it
            newGO.transform.parent = transform;
            // Add AudioSource
            AudioSource newSource = newGO.AddComponent<AudioSource>();
            // Set output
            newSource.outputAudioMixerGroup = audioMixerGroup;
            // Add to pool
            sources.Add(newSource);
            return newSource;
        }
    }
}