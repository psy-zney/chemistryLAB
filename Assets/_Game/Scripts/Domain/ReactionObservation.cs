using System;

namespace ChemistryLab.Domain
{
    public enum ReactionObservationType
    {
        ColorChange = 0,
        GasEvolution = 1,
        PrecipitateFormation = 2,
        Exothermic = 3,
        Endothermic = 4,
        LightEmission = 5,
        OdorChange = 6
    }

    public enum ReactionObservationTimingPhase
    {
        OnPour = 0,
        DuringReaction = 1,
        OnCompletion = 2
    }

    public enum ObservationIntensity
    {
        Subtle = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Dramatic = 4
    }

    /// <summary>
    /// One ordered, player-facing effect of a reaction. Asset keys are optional so
    /// low-end devices can fall back to the localised description alone.
    /// </summary>
    public sealed class ReactionObservation
    {
        public ReactionObservation(
            string reactionId,
            int sequence,
            ReactionObservationType type,
            ReactionObservationTimingPhase timingPhase,
            ObservationIntensity intensity,
            string beforeColor,
            string afterColor,
            string particleAssetKey,
            string audioAssetKey,
            string hapticAssetKey,
            string localisationKey,
            string sourceRef,
            string reviewer,
            DateTimeOffset reviewedAt,
            string contentVersion)
        {
            ReactionId = RequireText(reactionId, nameof(reactionId));
            Sequence = RequireNonNegative(sequence, nameof(sequence));
            Type = type;
            TimingPhase = timingPhase;
            Intensity = intensity;
            BeforeColor = beforeColor;
            AfterColor = afterColor;
            ParticleAssetKey = particleAssetKey;
            AudioAssetKey = audioAssetKey;
            HapticAssetKey = hapticAssetKey;
            LocalisationKey = RequireText(localisationKey, nameof(localisationKey));
            SourceRef = RequireText(sourceRef, nameof(sourceRef));
            Reviewer = RequireText(reviewer, nameof(reviewer));
            ReviewedAt = reviewedAt;
            ContentVersion = RequireText(contentVersion, nameof(contentVersion));
        }

        public string ReactionId { get; private set; }
        public int Sequence { get; private set; }
        public ReactionObservationType Type { get; private set; }
        public ReactionObservationTimingPhase TimingPhase { get; private set; }
        public ObservationIntensity Intensity { get; private set; }
        public string BeforeColor { get; private set; }
        public string AfterColor { get; private set; }
        public string ParticleAssetKey { get; private set; }
        public string AudioAssetKey { get; private set; }
        public string HapticAssetKey { get; private set; }
        public string LocalisationKey { get; private set; }
        public string SourceRef { get; private set; }
        public string Reviewer { get; private set; }
        public DateTimeOffset ReviewedAt { get; private set; }
        public string ContentVersion { get; private set; }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }
    }
}
