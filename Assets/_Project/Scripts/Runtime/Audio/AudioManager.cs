using System;
using System.Collections.Generic;
using CoinRush.Core;
using CoinRush.Data;
using UnityEngine;

namespace CoinRush.Audio
{
    /// <summary>
    /// Music + one-shot sound effects. Sounds are configured in the Inspector as
    /// (SoundId -> clip, volume, pitch variation). Static <see cref="Play"/> lets any script
    /// trigger a sound without a reference. Volumes follow <see cref="SettingsService"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        public class SoundEntry
        {
            public SoundId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Random +/- pitch applied each play so repeats feel less robotic.")]
            [Range(0f, 0.5f)] public float pitchVariation = 0.05f;
        }

        private static AudioManager instance;

        // Lets the same track continue seamlessly across scenes (Splash -> Story -> Menu).
        private static AudioClip lastMusic;
        private static float lastMusicTime;

        [Header("Music")]
        [SerializeField] private AudioClip musicClip;
        [Tooltip("Played when the final world is cleared.")]
        [SerializeField] private AudioClip victoryClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField, Min(0f)] private float musicFadeTime = 1.2f;

        private float musicFade = 1f;

        [Header("Sound Effects")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(1, 16)] private int voices = 8;
        [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();

        private AudioSource musicSource;
        private AudioSource[] sfxSources;
        private int nextVoice;
        private readonly Dictionary<SoundId, SoundEntry> lookup = new Dictionary<SoundId, SoundEntry>();

        private void Awake()
        {
            instance = this;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            // A small pool of sources so pitch changes on one sound don't affect others.
            sfxSources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                sfxSources[i] = gameObject.AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
            }

            lookup.Clear();
            foreach (SoundEntry entry in sounds)
                if (entry != null && entry.clip != null) lookup[entry.id] = entry;

            ApplyVolumes();
        }

        private void OnEnable()
        {
            SettingsService.Changed += ApplyVolumes;
            GameEvents.WorldStarted += HandleWorldStarted;
            GameEvents.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            SettingsService.Changed -= ApplyVolumes;
            GameEvents.WorldStarted -= HandleWorldStarted;
            GameEvents.StateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            if (musicClip == null) return;
            musicSource.clip = musicClip;
            if (musicClip == lastMusic)
            {
                musicSource.time = Mathf.Min(lastMusicTime, musicClip.length - 0.05f);
                musicFade = 1f; // already playing in the previous scene: no fade
            }
            else musicFade = 0f; // new track: fade in
            ApplyVolumes();
            musicSource.Play();
        }

        private void Update()
        {
            if (musicFade >= 1f || musicFadeTime <= 0f) return;
            musicFade = Mathf.Min(1f, musicFade + Time.unscaledDeltaTime / musicFadeTime);
            ApplyVolumes();
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Won || victoryClip == null) return;
            musicSource.clip = victoryClip;
            musicSource.pitch = 1f;
            musicFade = 0f;
            musicSource.Play();
        }

        private void OnDestroy()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                lastMusic = musicSource.clip;
                lastMusicTime = musicSource.time;
            }
            else lastMusic = null;
            if (instance == this) instance = null;
        }

        private void HandleWorldStarted(WorldConfig world, int index, int count) => musicSource.pitch = world.musicPitch;

        private void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = musicVolume * SettingsService.MusicVolume * musicFade;
        }

        /// <summary>Play a 2D one-shot. Safe to call when no AudioManager exists.</summary>
        public static void Play(SoundId id) => PlayPitched(id, 1f);

        /// <summary>Play with an extra pitch factor (used to make combo pickups rise in pitch).</summary>
        public static void PlayPitched(SoundId id, float pitch)
        {
            if (instance != null) instance.PlayInternal(id, pitch);
        }

        private void PlayInternal(SoundId id, float pitch)
        {
            if (id == SoundId.None || !lookup.TryGetValue(id, out SoundEntry entry)) return;

            AudioSource source = sfxSources[nextVoice];
            nextVoice = (nextVoice + 1) % sfxSources.Length;
            source.pitch = pitch * (1f + UnityEngine.Random.Range(-entry.pitchVariation, entry.pitchVariation));
            source.PlayOneShot(entry.clip, entry.volume * sfxVolume * SettingsService.SfxVolume);
        }
    }
}
