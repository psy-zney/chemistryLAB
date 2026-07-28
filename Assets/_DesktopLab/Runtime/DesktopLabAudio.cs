using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// Original procedural soundscape for the desktop lab.
    /// All waveforms are generated at runtime; the build ships without licensed audio files.
    /// </summary>
    public sealed class DesktopLabAudio : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const string MutedKey = "chemistryLab.desktop.audioMuted";
        private const float MusicVolume = 0.075f;
        private const float AmbienceVolume = 0.105f;

        private readonly Dictionary<string, AudioClip> clips =
            new Dictionary<string, AudioClip>(StringComparer.Ordinal);

        private AudioSource musicSource;
        private AudioSource ambienceSource;
        private AudioSource uiSource;
        private AudioSource worldSource;
        private AudioSource footstepSource;
        private bool paused;
        private float nextHoverAt;

        public bool IsMuted { get; private set; }
        public bool Ready { get; private set; }
        public int ClipCount { get { return clips.Count; } }

        public string StatusLabel
        {
            get
            {
                if (!Ready)
                {
                    return "CHƯA SẴN SÀNG";
                }

                return IsMuted ? "TẮT" : "BẬT";
            }
        }

        public void Initialise()
        {
            IsMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            BuildLibrary();

            musicSource = CreateSource("Procedural Music", false, 0f);
            ambienceSource = CreateSource("Laboratory Ambience", false, 0f);
            uiSource = CreateSource("UI Feedback", false, 0f);
            worldSource = CreateSource("Reaction Audio", true, 7.5f);
            footstepSource = CreateSource("Footsteps", false, 0f);

            musicSource.clip = clips["music"];
            musicSource.loop = true;
            musicSource.volume = MusicVolume;
            ambienceSource.clip = clips["ambience"];
            ambienceSource.loop = true;
            ambienceSource.volume = AmbienceVolume;
            ApplyMute();
            musicSource.Play();
            ambienceSource.Play();
            Ready = clips.Count == 14;
        }

        private void Update()
        {
            if (musicSource == null || ambienceSource == null)
            {
                return;
            }

            var musicTarget = paused ? MusicVolume * 0.42f : MusicVolume;
            var ambienceTarget = paused ? AmbienceVolume * 0.58f : AmbienceVolume;
            musicSource.volume = Mathf.MoveTowards(
                musicSource.volume,
                musicTarget,
                Time.unscaledDeltaTime * 0.16f);
            ambienceSource.volume = Mathf.MoveTowards(
                ambienceSource.volume,
                ambienceTarget,
                Time.unscaledDeltaTime * 0.2f);
        }

        public void SetPaused(bool value)
        {
            paused = value;
        }

        public void ToggleMuted()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt(MutedKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMute();

            if (!IsMuted)
            {
                PlayUi("ui-click", 0.62f);
            }
        }

        public void PlayUiHover()
        {
            if (Time.unscaledTime < nextHoverAt)
            {
                return;
            }

            nextHoverAt = Time.unscaledTime + 0.055f;
            PlayUi("ui-hover", 0.34f);
        }

        public void PlayUiClick()
        {
            PlayUi("ui-click", 0.56f);
        }

        public void PlayError()
        {
            PlayUi("error", 0.48f);
        }

        public void PlaySamplePickup()
        {
            PlayUi("pickup", 0.42f);
        }

        public void PlayPour(Vector3 position)
        {
            PlayWorld("pour", position, 0.62f);
        }

        public void PlayWash(Vector3 position)
        {
            PlayWorld("wash", position, 0.54f);
        }

        public void PlayFootstep(bool running)
        {
            if (!Ready || footstepSource == null || IsMuted)
            {
                return;
            }

            footstepSource.pitch = running
                ? UnityEngine.Random.Range(1.04f, 1.1f)
                : UnityEngine.Random.Range(0.94f, 1.02f);
            footstepSource.PlayOneShot(clips["footstep"], running ? 0.34f : 0.26f);
        }

        public void PlayReaction(ReactionEffect effect, Vector3 position, float temperatureDelta)
        {
            var clipName = "reaction-colour";
            switch (effect)
            {
                case ReactionEffect.Heat:
                    clipName = "reaction-heat";
                    break;
                case ReactionEffect.Precipitate:
                    clipName = "reaction-precipitate";
                    break;
                case ReactionEffect.Gas:
                    clipName = "reaction-gas";
                    break;
            }

            var intensity = Mathf.Clamp01(0.48f + Mathf.Abs(temperatureDelta) / 34f);
            PlayWorld(clipName, position, intensity);
        }

        public static void ValidateSignalGenerationOrThrow()
        {
            var waveforms = new[]
            {
                GenerateMusic(0.4f),
                GenerateAmbience(0.4f),
                GenerateUiClick(),
                GenerateReactionGas()
            };

            foreach (var samples in waveforms)
            {
                var peak = 0f;
                var energy = 0d;
                for (var index = 0; index < samples.Length; index++)
                {
                    var sample = samples[index];
                    if (float.IsNaN(sample) || float.IsInfinity(sample))
                    {
                        throw new InvalidOperationException("Procedural audio contains a non-finite sample.");
                    }

                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                    energy += sample * sample;
                }

                if (peak > 1.001f || energy / samples.Length < 0.0000001d)
                {
                    throw new InvalidOperationException(
                        "Procedural audio signal is invalid. peak=" + peak + " energy=" + energy);
                }
            }
        }

        private void BuildLibrary()
        {
            AddClip("music", GenerateMusic(16f));
            AddClip("ambience", GenerateAmbience(8f));
            AddClip("ui-hover", GenerateChime(880f, 0.055f, 0.16f));
            AddClip("ui-click", GenerateUiClick());
            AddClip("error", GenerateError());
            AddClip("pickup", GeneratePickup());
            AddClip("pour", GeneratePour());
            AddClip("wash", GenerateWash());
            AddClip("footstep", GenerateFootstep());
            AddClip("reaction-heat", GenerateReactionHeat());
            AddClip("reaction-precipitate", GenerateReactionPrecipitate());
            AddClip("reaction-gas", GenerateReactionGas());
            AddClip("reaction-colour", GenerateReactionColour());
            AddClip("pause", GenerateChime(440f, 0.18f, 0.3f));
        }

        private void AddClip(string id, float[] samples)
        {
            var clip = AudioClip.Create(
                "Chemistry Lab · " + id,
                samples.Length,
                1,
                SampleRate,
                false);
            if (!clip.SetData(samples, 0))
            {
                throw new InvalidOperationException("Could not initialise procedural audio clip: " + id);
            }

            clips.Add(id, clip);
        }

        private AudioSource CreateSource(string sourceName, bool spatial, float maxDistance)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.spatialBlend = spatial ? 0.92f : 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 0.8f;
            source.maxDistance = spatial ? maxDistance : 500f;
            return source;
        }

        private void ApplyMute()
        {
            foreach (var source in GetComponentsInChildren<AudioSource>(true))
            {
                source.mute = IsMuted;
            }
        }

        private void PlayUi(string id, float volume)
        {
            AudioClip clip;
            if (!Ready || uiSource == null || IsMuted || !clips.TryGetValue(id, out clip))
            {
                return;
            }

            uiSource.pitch = 1f;
            uiSource.PlayOneShot(clip, volume);
        }

        private void PlayWorld(string id, Vector3 position, float volume)
        {
            AudioClip clip;
            if (!Ready || worldSource == null || IsMuted || !clips.TryGetValue(id, out clip))
            {
                return;
            }

            worldSource.transform.position = position;
            worldSource.pitch = UnityEngine.Random.Range(0.97f, 1.035f);
            worldSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static float[] GenerateMusic(float duration)
        {
            var samples = NewBuffer(duration);
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                var phrase = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * t / duration);
                var breathe = 0.74f + 0.26f * Mathf.Sin(2f * Mathf.PI * t * 0.125f);
                var bass = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.22f;
                var fifth = Mathf.Sin(2f * Mathf.PI * 82.5f * t + 0.3f) * 0.12f;
                var glass = Mathf.Sin(2f * Mathf.PI * 220f * t + Mathf.Sin(t * 0.7f) * 0.3f) * 0.035f;
                samples[index] = SoftClip((bass + fifth + glass) * (0.42f + 0.58f * phrase) * breathe);
            }

            return samples;
        }

        private static float[] GenerateAmbience(float duration)
        {
            var samples = NewBuffer(duration);
            var filteredNoise = 0f;
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                filteredNoise = Mathf.Lerp(filteredNoise, SignedNoise(index, 7309), 0.018f);
                var fan = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.08f
                    + Mathf.Sin(2f * Mathf.PI * 96f * t) * 0.025f;
                samples[index] = SoftClip(fan + filteredNoise * 0.11f);
            }

            return samples;
        }

        private static float[] GenerateUiClick()
        {
            var samples = NewBuffer(0.095f);
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                var envelope = Mathf.Exp(-t * 38f);
                samples[index] = SoftClip(
                    (Mathf.Sin(2f * Mathf.PI * 620f * t) * 0.38f
                     + Mathf.Sin(2f * Mathf.PI * 930f * t) * 0.18f) * envelope);
            }

            return samples;
        }

        private static float[] GenerateChime(float frequency, float duration, float gain)
        {
            var samples = NewBuffer(duration);
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration))
                    * Mathf.Exp(-t * 7f);
                samples[index] = SoftClip(
                    (Mathf.Sin(2f * Mathf.PI * frequency * t)
                     + 0.3f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * t))
                    * gain * envelope);
            }

            return samples;
        }

        private static float[] GenerateError()
        {
            var samples = NewBuffer(0.24f);
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                var envelope = Mathf.Exp(-t * 9f);
                var frequency = t < 0.1f ? 230f : 185f;
                samples[index] = SoftClip(Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.36f);
            }

            return samples;
        }

        private static float[] GeneratePickup()
        {
            var samples = NewBuffer(0.28f);
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                var envelope = Mathf.Exp(-t * 8f);
                var tone = Mathf.Sin(2f * Mathf.PI * (520f + t * 620f) * t);
                samples[index] = SoftClip(tone * envelope * 0.32f);
            }

            return samples;
        }

        private static float[] GeneratePour()
        {
            var samples = NewBuffer(0.72f);
            var filteredNoise = 0f;
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                filteredNoise = Mathf.Lerp(filteredNoise, SignedNoise(index, 1193), 0.2f);
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.72f));
                var bubble = Mathf.Sin(2f * Mathf.PI * (130f + 45f * Mathf.Sin(t * 21f)) * t) * 0.08f;
                samples[index] = SoftClip((filteredNoise * 0.3f + bubble) * envelope);
            }

            return samples;
        }

        private static float[] GenerateWash()
        {
            var samples = NewBuffer(1.05f);
            var filteredNoise = 0f;
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                filteredNoise = Mathf.Lerp(filteredNoise, SignedNoise(index, 4021), 0.3f);
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.05f));
                var water = filteredNoise * 0.35f
                    + Mathf.Sin(2f * Mathf.PI * 780f * t) * 0.025f;
                samples[index] = SoftClip(water * envelope);
            }

            return samples;
        }

        private static float[] GenerateFootstep()
        {
            var samples = NewBuffer(0.2f);
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                var envelope = Mathf.Exp(-t * 24f);
                var thump = Mathf.Sin(2f * Mathf.PI * (92f - t * 110f) * t) * 0.44f;
                var scuff = SignedNoise(index, 9187) * Mathf.Exp(-t * 42f) * 0.1f;
                samples[index] = SoftClip((thump + scuff) * envelope);
            }

            return samples;
        }

        private static float[] GenerateReactionHeat()
        {
            var samples = NewBuffer(1.15f);
            var filteredNoise = 0f;
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                filteredNoise = Mathf.Lerp(filteredNoise, SignedNoise(index, 6211), 0.08f);
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.15f));
                var rumble = Mathf.Sin(2f * Mathf.PI * (74f + t * 24f) * t) * 0.18f;
                samples[index] = SoftClip((filteredNoise * 0.25f + rumble) * envelope);
            }

            return samples;
        }

        private static float[] GenerateReactionPrecipitate()
        {
            var samples = NewBuffer(0.92f);
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                var envelope = Mathf.Exp(-t * 3.2f);
                var grains = SignedNoise(index, 2887) * (0.04f + 0.18f * Mathf.Abs(Mathf.Sin(t * 37f)));
                var clink = Mathf.Sin(2f * Mathf.PI * 360f * t) * 0.12f;
                samples[index] = SoftClip((grains + clink) * envelope);
            }

            return samples;
        }

        private static float[] GenerateReactionGas()
        {
            var samples = NewBuffer(1.25f);
            var filteredNoise = 0f;
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                filteredNoise = Mathf.Lerp(filteredNoise, SignedNoise(index, 5573), 0.24f);
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.25f));
                var bubbles = Mathf.Sin(
                    2f * Mathf.PI * (180f + 70f * Mathf.Sin(2f * Mathf.PI * 5f * t)) * t) * 0.09f;
                samples[index] = SoftClip((filteredNoise * 0.34f + bubbles) * envelope);
            }

            return samples;
        }

        private static float[] GenerateReactionColour()
        {
            var samples = NewBuffer(0.82f);
            for (var index = 0; index < samples.Length; index++)
            {
                var t = index / (float)SampleRate;
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.82f));
                var shimmer = Mathf.Sin(2f * Mathf.PI * 440f * t)
                    + 0.46f * Mathf.Sin(2f * Mathf.PI * 660f * t)
                    + 0.2f * Mathf.Sin(2f * Mathf.PI * 990f * t);
                samples[index] = SoftClip(shimmer * envelope * 0.19f);
            }

            return samples;
        }

        private static float[] NewBuffer(float duration)
        {
            return new float[Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate))];
        }

        private static float SignedNoise(int index, int seed)
        {
            unchecked
            {
                var value = (uint)(index + seed);
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return (value / (float)uint.MaxValue) * 2f - 1f;
            }
        }

        private static float SoftClip(float sample)
        {
            return Mathf.Clamp(sample / (1f + Mathf.Abs(sample)), -0.96f, 0.96f);
        }
    }
}
