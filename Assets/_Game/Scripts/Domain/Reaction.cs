using System;

namespace ChemistryLab.Domain
{
    /// <summary>
    /// A reviewed, catalogue-defined reaction. Resolution is intentionally driven by
    /// imported participants rather than chemical inference at runtime.
    /// </summary>
    public sealed class Reaction
    {
        public Reaction(
            string id,
            string equationDisplay,
            string conditionKey,
            string toolId,
            int unlockLevel,
            string outputId,
            decimal outputMassGram,
            decimal animationDurationSeconds,
            string sourceRef,
            string reviewer,
            DateTimeOffset reviewedAt,
            string contentVersion)
        {
            Id = RequireText(id, nameof(id));
            EquationDisplay = RequireText(equationDisplay, nameof(equationDisplay));
            ConditionKey = RequireText(conditionKey, nameof(conditionKey));
            ToolId = RequireText(toolId, nameof(toolId));
            UnlockLevel = RequireNonNegative(unlockLevel, nameof(unlockLevel));
            OutputId = RequireText(outputId, nameof(outputId));
            OutputMassGram = RequirePositive(outputMassGram, nameof(outputMassGram));
            AnimationDurationSeconds = RequireNonNegative(animationDurationSeconds, nameof(animationDurationSeconds));
            SourceRef = RequireText(sourceRef, nameof(sourceRef));
            Reviewer = RequireText(reviewer, nameof(reviewer));
            ReviewedAt = reviewedAt;
            ContentVersion = RequireText(contentVersion, nameof(contentVersion));
        }

        public string Id { get; private set; }
        public string EquationDisplay { get; private set; }
        public string ConditionKey { get; private set; }
        public string ToolId { get; private set; }
        public int UnlockLevel { get; private set; }
        public string OutputId { get; private set; }
        public decimal OutputMassGram { get; private set; }
        public decimal AnimationDurationSeconds { get; private set; }
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

        private static decimal RequirePositive(decimal value, string parameterName)
        {
            if (value <= 0m)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }

        private static decimal RequireNonNegative(decimal value, string parameterName)
        {
            if (value < 0m)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }
    }
}
