using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChemistryLab.Presentation
{
    public enum AudioChannel { Music = 0, SFX = 1, UI = 2, Incident = 3 }
    public enum HapticType { Pour = 0, ReactionComplete = 1, Warning = 2 }

    /// <summary>Implemented by the Unity audio/haptic adapter at the presentation edge.</summary>
    public interface IAudioHapticFeedback
    {
        void ApplyAudio(AudioChannel channel, bool isMuted, float volume);
        void TriggerHaptic(HapticType type);
    }

    public sealed class AudioHapticSettings
    {
        internal AudioHapticSettings(bool isMuted, bool hapticsEnabled, IDictionary<AudioChannel, float> volumes)
        {
            IsMuted = isMuted;
            HapticsEnabled = hapticsEnabled;
            Volumes = new Dictionary<AudioChannel, float>(volumes);
        }

        public bool IsMuted { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public IReadOnlyDictionary<AudioChannel, float> Volumes { get; private set; }
    }

    /// <summary>Owns user audio/haptic preferences and applies them immediately.</summary>
    public sealed class AudioHapticController
    {
        private const string MuteKey = "audio.muted";
        private const string HapticsKey = "haptics.enabled";
        private const string VolumeKeyPrefix = "audio.volume.";
        private readonly IPresentationSettingsStore settingsStore;
        private readonly IAudioHapticFeedback feedback;
        private readonly Func<bool> isHapticAllowed;
        private readonly Dictionary<AudioChannel, float> volumes = new Dictionary<AudioChannel, float>();

        public AudioHapticController(IPresentationSettingsStore settingsStore = null, IAudioHapticFeedback feedback = null, Func<bool> isHapticAllowed = null)
        {
            this.settingsStore = settingsStore ?? PresentationSettings.DefaultStore;
            this.feedback = feedback;
            this.isHapticAllowed = isHapticAllowed ?? delegate { return true; };
            IsMuted = ReadBoolean(MuteKey, false);
            HapticsEnabled = ReadBoolean(HapticsKey, true);
            foreach (AudioChannel channel in Enum.GetValues(typeof(AudioChannel)))
                volumes[channel] = ReadVolume(channel);
            ApplyAllAudio();
        }

        public bool IsMuted { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public AudioHapticSettings Settings { get { return new AudioHapticSettings(IsMuted, HapticsEnabled, volumes); } }

        public event Action<AudioHapticSettings> SettingsChanged;
        public event Action<HapticType> HapticTriggered;

        public void SetMute(bool isMuted)
        {
            if (IsMuted == isMuted) return;
            IsMuted = isMuted;
            settingsStore.SetString(MuteKey, isMuted ? "true" : "false");
            ApplyAllAudio();
            PublishSettingsChanged();
        }

        public void SetVolume(string channel, float volume)
        {
            AudioChannel parsedChannel;
            if (!TryParseChannel(channel, out parsedChannel)) throw new ArgumentException("Unknown audio channel.", nameof(channel));
            if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0f || volume > 1f)
                throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 1.");
            if (volumes[parsedChannel] == volume) return;
            volumes[parsedChannel] = volume;
            settingsStore.SetString(VolumeKeyPrefix + parsedChannel, volume.ToString("R", CultureInfo.InvariantCulture));
            if (feedback != null) feedback.ApplyAudio(parsedChannel, IsMuted, volume);
            PublishSettingsChanged();
        }

        public void SetHapticsEnabled(bool isEnabled)
        {
            if (HapticsEnabled == isEnabled) return;
            HapticsEnabled = isEnabled;
            settingsStore.SetString(HapticsKey, isEnabled ? "true" : "false");
            PublishSettingsChanged();
        }

        public void TriggerHaptic(HapticType type)
        {
            if (!HapticsEnabled || !isHapticAllowed()) return;
            if (feedback != null) feedback.TriggerHaptic(type);
            var triggered = HapticTriggered;
            if (triggered != null) triggered(type);
        }

        private void ApplyAllAudio()
        {
            if (feedback == null) return;
            foreach (var entry in volumes) feedback.ApplyAudio(entry.Key, IsMuted, entry.Value);
        }

        private float ReadVolume(AudioChannel channel)
        {
            var text = settingsStore.GetString(VolumeKeyPrefix + channel);
            float value;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= 0f && value <= 1f ? value : 1f;
        }

        private bool ReadBoolean(string key, bool fallback)
        {
            var value = settingsStore.GetString(key);
            bool parsed;
            return bool.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static bool TryParseChannel(string value, out AudioChannel channel)
        {
            channel = AudioChannel.Music;
            if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value, true, out channel)) return false;
            return Enum.IsDefined(typeof(AudioChannel), channel);
        }

        private void PublishSettingsChanged()
        {
            var changed = SettingsChanged;
            if (changed != null) changed(Settings);
        }
    }
}
