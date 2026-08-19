using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace toolbox
{
    /// <summary>
    /// A small bank of clips and the voices to play them on.
    ///
    /// Drop it on anything that makes a repeated noise — footsteps, impacts, a door — give it a
    /// handful of takes, and call <see cref="PlayRandomOneShot()"/>. It owns a fixed pool of
    /// <see cref="AudioSource"/>s so overlapping sounds layer instead of cutting each other off,
    /// and it picks clips from a shuffle bag so a sound repeated quickly does not obviously repeat.
    ///
    /// WHY A SHUFFLE BAG. Uniform random picks the same clip twice in a row one time in n, and that
    /// specific artefact — not the size of the bank — is what the ear hears as a machine gun. A bag
    /// plays every clip once before any clip plays twice, so the worst case is the seam between two
    /// bags, and even that is guarded (see <see cref="Reshuffle"/>).
    /// </summary>
    [AddComponentMenu("Toolbox/Sound Box")]
    public class SoundBox : MonoBehaviour
    {
        [Tooltip("Alternate takes of the same sound. One clip works; three or four is where the " +
                 "repetition stops being audible.")]
        public List<AudioClip> sounds = new List<AudioClip>();

        [Header("Variation")]
        [Tooltip("Random pitch range, as a multiplier. A little either side of 1 is enough — past " +
                 "about 0.85-1.15 the sound starts changing size rather than just varying.")]
        public Vector2 pitchRange = new Vector2(0.92f, 1.08f);

        [Tooltip("Random volume range. Kept subtle for the same reason as pitch.")]
        public Vector2 volumeRange = new Vector2(0.9f, 1f);

        [Header("Voices")]
        [Tooltip("How many sounds can overlap. When they are all busy the oldest is stolen, so a " +
                 "burst of hits sounds like a burst rather than silence.\n\n" +
                 "Small on purpose: unbounded voices is how a fight turns to mud.")]
        [Range(1, 16)] public int voices = 4;

        [Tooltip("Seconds this box refuses to speak again. Guards against a caller that fires " +
                 "several times in one frame — an oversampled sweep, a multi-collider hit. Zero " +
                 "disables it.")]
        [Min(0f)] public float minimumInterval = 0f;

        [Header("Space")]
        [Tooltip("0 is heard everywhere at full volume, 1 is positioned in the world. Impacts and " +
                 "footsteps want 1; UI and music want 0.")]
        [Range(0f, 1f)] public float spatialBlend = 1f;

        [Tooltip("Full volume inside this radius, in metres.")]
        [Min(0.01f)] public float minDistance = 1f;

        [Tooltip("Silent past this radius, in metres.")]
        [Min(0.02f)] public float maxDistance = 30f;

        [Tooltip("Linear reads as a predictable falloff over a known distance, which is usually " +
                 "what a gameplay sound wants. Logarithmic is physically truer and much harder to " +
                 "mix.")]
        public AudioRolloffMode rolloff = AudioRolloffMode.Linear;

        [Tooltip("Leave empty for the default group.")]
        public AudioMixerGroup output;

        public bool playOnAwake = false;

        private readonly List<AudioSource> audioSources = new List<AudioSource>();
        private float[] _startedAt;
        private int[] _bag;
        private int _bagRemaining;
        private int _lastPlayed = -1;
        private float _nextAllowed = float.NegativeInfinity;
        private int _nextVoice;

        /// <summary>The voices this box owns. Empty until it has woken.</summary>
        public IReadOnlyList<AudioSource> Voices => audioSources;

        private void Awake()
        {
            BuildVoices();
        }

        private void Start()
        {
            if (playOnAwake)
                PlayRandomOneShot();
        }

        private void OnValidate()
        {
            // Keep the range fields honest whichever order they were typed in — the original bug
            // here was a default of (1, 0) read as y-to-x, which lerped down to silence.
            if (pitchRange.y < pitchRange.x)
                pitchRange = new Vector2(pitchRange.y, pitchRange.x);
            if (volumeRange.y < volumeRange.x)
                volumeRange = new Vector2(volumeRange.y, volumeRange.x);
            if (maxDistance <= minDistance)
                maxDistance = minDistance + 0.01f;
        }

        /// <summary>Plays a clip from the bag on the next free voice.</summary>
        public void PlayRandomOneShot() => Play(NextClip());

        /// <summary>
        /// Plays a clip from the bag at a chosen point in the pitch range.
        ///
        /// <paramref name="pitchModulate"/> runs 0 (the low end of <see cref="pitchRange"/>) to 1
        /// (the high end) — pass how hard the thing hit, how fast it was going, how full the
        /// container is. Outside that the range still applies, it is just extrapolated.
        /// </summary>
        public void PlayRandomOneShot(float pitchModulate)
            => Play(NextClip(), 1f, Mathf.LerpUnclamped(pitchRange.x, pitchRange.y, pitchModulate));

        /// <summary>Plays a clip from the bag at a world point rather than at this transform.</summary>
        public void PlayRandomOneShotAt(Vector3 worldPosition)
            => Play(NextClip(), 1f, float.NaN, worldPosition);

        /// <summary>
        /// Plays a clip from the bag at a world point with the volume and pitch decided by the
        /// caller, for anything that already knows how loud and how deep this one should be.
        ///
        /// The random ranges still apply on top of the volume, so set <see cref="volumeRange"/> to
        /// (1, 1) on a box whose callers do their own scaling.
        /// </summary>
        public void PlayRandomOneShotAt(Vector3 worldPosition, float volumeScale, float pitch)
            => Play(NextClip(), volumeScale, pitch, worldPosition);

        /// <summary>Plays a specific clip through this box's voices, variation and mixer group.</summary>
        public void Play(AudioClip clip, float volumeScale = 1f)
            => Play(clip, volumeScale, float.NaN);

        /// <summary>Plays a specific clip at a world point.</summary>
        public void PlayAt(AudioClip clip, Vector3 worldPosition, float volumeScale = 1f)
            => Play(clip, volumeScale, float.NaN, worldPosition);

        /// <summary>Stops every voice immediately.</summary>
        public void StopAll()
        {
            for (int i = 0; i < audioSources.Count; i++)
            {
                if (audioSources[i] != null)
                    audioSources[i].Stop();
            }
        }

        /// <summary>
        /// The one place a sound actually starts.
        ///
        /// <paramref name="pitch"/> is NaN for "pick one from the range", which is how the callers
        /// that do not care about pitch stay at one argument.
        /// </summary>
        private void Play(AudioClip clip, float volumeScale, float pitch, Vector3? worldPosition = null)
        {
            if (clip == null || volumeScale <= 0f)
                return;

            // Unscaled: a sound bank throttling itself has nothing to do with game time, and this
            // has to keep working while the game is paused or in slow motion.
            float now = Time.unscaledTime;
            if (minimumInterval > 0f && now < _nextAllowed)
                return;

            _nextAllowed = now + minimumInterval;

            AudioSource voice = TakeVoice(now);
            if (voice == null)
                return;

            if (worldPosition.HasValue)
                voice.transform.position = worldPosition.Value;
            else
                voice.transform.localPosition = Vector3.zero;

            voice.pitch = float.IsNaN(pitch) ? Random.Range(pitchRange.x, pitchRange.y) : pitch;

            // PlayOneShot rather than clip + Play: one-shots are not cut off when the voice is
            // reused, and the volume argument scales this shot alone rather than the source.
            voice.PlayOneShot(clip, Random.Range(volumeRange.x, volumeRange.y) * volumeScale);
        }

        /// <summary>
        /// A voice that is free, or the one that has been going longest.
        ///
        /// Stealing the oldest rather than refusing to play is deliberate: an impact you can hear
        /// half of reads as an impact, and one that never starts reads as a missing sound.
        /// </summary>
        private AudioSource TakeVoice(float now)
        {
            if (audioSources.Count == 0)
                BuildVoices();
            if (audioSources.Count == 0)
                return null;

            // Round-robin the starting point so a bank that is never saturated still spreads its
            // sounds over every voice, instead of hammering voice 0 and leaving the rest silent.
            int count = audioSources.Count;
            for (int i = 0; i < count; i++)
            {
                int index = (_nextVoice + i) % count;
                AudioSource candidate = audioSources[index];
                if (candidate != null && !candidate.isPlaying)
                {
                    _nextVoice = (index + 1) % count;
                    _startedAt[index] = now;
                    return candidate;
                }
            }

            int oldest = 0;
            for (int i = 1; i < count; i++)
            {
                if (_startedAt[i] < _startedAt[oldest])
                    oldest = i;
            }

            _startedAt[oldest] = now;
            _nextVoice = (oldest + 1) % count;
            audioSources[oldest].Stop();
            return audioSources[oldest];
        }

        /// <summary>
        /// Builds the pool once, as child objects.
        ///
        /// Children rather than components on this object so a voice can be moved to a contact
        /// point without dragging the thing that owns it.
        /// </summary>
        private void BuildVoices()
        {
            if (audioSources.Count > 0)
                return;

            int wanted = Mathf.Max(1, voices);
            _startedAt = new float[wanted];

            for (int i = 0; i < wanted; i++)
            {
                var host = new GameObject("Voice " + i);
                host.transform.SetParent(transform, false);

                AudioSource source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = spatialBlend;
                source.minDistance = minDistance;
                source.maxDistance = maxDistance;
                source.rolloffMode = rolloff;
                source.dopplerLevel = 0f;   // A sword hit is not a passing ambulance.
                source.outputAudioMixerGroup = output;

                audioSources.Add(source);
                _startedAt[i] = float.NegativeInfinity;
            }
        }

        /// <summary>Draws the next clip from the shuffle bag, refilling it when it runs out.</summary>
        private AudioClip NextClip()
        {
            if (sounds.Count == 0)
                return null;
            if (sounds.Count == 1)
                return sounds[0];

            if (_bag == null || _bag.Length != sounds.Count || _bagRemaining <= 0)
                Reshuffle();

            _bagRemaining--;
            int pick = _bag[_bagRemaining];
            _lastPlayed = pick;
            return sounds[pick];
        }

        private void Reshuffle()
        {
            if (_bag == null || _bag.Length != sounds.Count)
            {
                _bag = new int[sounds.Count];
                for (int i = 0; i < _bag.Length; i++)
                    _bag[i] = i;
            }

            for (int i = _bag.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }

            // The bag is drawn from the END, so the last entry plays next. If that is what just
            // played, swap it away — the seam between two bags is the only place a repeat can
            // happen, and this closes it.
            int last = _bag.Length - 1;
            if (_bag.Length > 1 && _bag[last] == _lastPlayed)
                (_bag[last], _bag[0]) = (_bag[0], _bag[last]);

            _bagRemaining = _bag.Length;
        }
    }
}
