using System;
using UnityEngine;
using UnityEngine.Audio;

namespace SV
{



    [CreateAssetMenu]
    public class AudioSFX : AssetWithGuid
    {
        [Range(0, 1)]
        public float volume;

        public AudioClip clip;
        public AudioMixerGroup mixer;
        internal void Play(AudioSource source)
        {
            source.clip = clip;
            source.volume = volume;
            source.outputAudioMixerGroup = mixer;
            source.Play();
        }
    }

}
