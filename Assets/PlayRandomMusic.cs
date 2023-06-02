using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SV
{


    public class PlayRandomMusic : MonoBehaviour
    {
        public AudioSFX[] tracks;
        private void Start()
        {
            AudioManager.Instance.PlayMusic(tracks[Random.Range(0, tracks.Length)]);
        }
    }
}
