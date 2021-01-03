using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wrj
{
    [RequireComponent(typeof(AudioPool))]
    public class RandomizedSoundEffect : MonoBehaviour
    {
        [SerializeField]
        private List<AudioClip> audioClips = new List<AudioClip>();
        private AudioPool m_AudioPool;

        void Start()
        {
            m_AudioPool = gameObject.EnsureComponent<AudioPool>();
        }

        public void PlayRandom(float pitchMin = 1f, float pitchMax = 1f, float volumeMin = 1f, float volumeMax = 1f)
        {
            if (audioClips.Count == 0 || audioClips == null)
                return;
            m_AudioPool.PlayPitchOneShot(audioClips.GetRandom(), Random.Range(pitchMin, pitchMax), Random.Range(volumeMin, volumeMax));
        }
        public void PlayRandom()
        {
            if (audioClips.Count == 0 || audioClips == null)
                return;
            m_AudioPool.PlayOneShot(audioClips.GetRandom());
        }
    }
}
