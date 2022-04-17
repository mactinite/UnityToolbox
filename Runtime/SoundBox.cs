using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace mactinite.ToolboxCommons
{
    public class SoundBox : MonoBehaviour
    {
        public List<AudioClip> sounds = new List<AudioClip>();
        private List<AudioSource> audioSources = new List<AudioSource>();
        public Vector2 pitchRange = Vector2.right;

        public bool playOnAwake = false;
        // Start is called before the first frame update
        void Start()
        {
            audioSources.Add(gameObject.AddComponent<AudioSource>());


            if (playOnAwake)
            {
                PlayRandomOneShot();
            }
        }

        public void PlayRandomOneShot()
        {
            if (sounds.Count == 0) return;

            AudioClip clip = sounds[Random.Range(0, sounds.Count)];

            for (int i = 0; i < audioSources.Count; i++)
            {
                if (!audioSources[i].isPlaying)
                {
                    audioSources[i].clip = clip;
                    audioSources[i].playOnAwake = false;
                    audioSources[i].loop = false;
                    audioSources[i].Play();
                    return;
                }
            }
            var newSource = gameObject.AddComponent<AudioSource>();
            newSource.clip = clip;
            newSource.loop = false;
            newSource.Play();
            audioSources.Add(newSource);

        }


        public void PlayRandomOneShot(float pitchModulate)
        {
            audioSources[0].pitch = Mathf.Lerp(pitchRange.y, pitchRange.x, pitchModulate);
            PlayRandomOneShot();
        }
    }
}
