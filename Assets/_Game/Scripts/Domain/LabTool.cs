using System;
using System.Collections.Generic;

namespace ChemistryLab.Domain
{
    public enum LabToolType
    {
        Container = 0,
        Handling = 1,
        Cleaning = 2,
        Filtration = 3,
        SynthesisMachine = 4
    }

    public enum ToolCleanState
    {
        Clean = 0,
        Dirty = 1,
        NeedsWashing = 2
    }

    /// <summary>
    /// Machine-readable capabilities used by reactions instead of string conditions.
    /// </summary>
    public enum LabToolCapability
    {
        Contain = 0,
        Measure = 1,
        Pour = 2,
        Stir = 3,
        Heat = 4,
        Cool = 5,
        Wash = 6,
        Filter = 7,
        Synthesize = 8,
        Electrolyze = 9
    }

    /// <summary>
    /// A catalogue tool or machine, including its initial ownership and cleanliness state.
    /// Runtime save data may override those two state values per player.
    /// </summary>
    public sealed class LabTool
    {
        public LabTool(
            string id,
            string nameKey,
            LabToolType type,
            decimal capacityGram,
            bool isOwned,
            ToolCleanState cleanState,
            int unlockLevel,
            long price,
            string visualAddressKey,
            IEnumerable<LabToolCapability> capabilities,
            string sourceRef,
            string reviewer,
            DateTimeOffset reviewedAt,
            string contentVersion)
        {
            Id = RequireText(id, nameof(id));
            NameKey = RequireText(nameKey, nameof(nameKey));
            Type = type;
            CapacityGram = RequireNonNegative(capacityGram, nameof(capacityGram));
            IsOwned = isOwned;
            CleanState = cleanState;
            UnlockLevel = RequireNonNegative(unlockLevel, nameof(unlockLevel));
            Price = RequireNonNegative(price, nameof(price));
            VisualAddressKey = RequireText(visualAddressKey, nameof(visualAddressKey));
            Capabilities = CopyCapabilities(capabilities);
            SourceRef = RequireText(sourceRef, nameof(sourceRef));
            Reviewer = RequireText(reviewer, nameof(reviewer));
            ReviewedAt = reviewedAt;
            ContentVersion = RequireText(contentVersion, nameof(contentVersion));
        }

        public string Id { get; private set; }
        public string NameKey { get; private set; }
        public LabToolType Type { get; private set; }
        public decimal CapacityGram { get; private set; }
        public bool IsOwned { get; private set; }
        public ToolCleanState CleanState { get; private set; }
        public int UnlockLevel { get; private set; }
        public long Price { get; private set; }
        public string VisualAddressKey { get; private set; }
        public IReadOnlyList<LabToolCapability> Capabilities { get; private set; }
        public string SourceRef { get; private set; }
        public string Reviewer { get; private set; }
        public DateTimeOffset ReviewedAt { get; private set; }
        public string ContentVersion { get; private set; }

        private static IReadOnlyList<LabToolCapability> CopyCapabilities(IEnumerable<LabToolCapability> capabilities)
        {
            if (capabilities == null)
            {
                throw new ArgumentNullException(nameof(capabilities));
            }

            var result = new List<LabToolCapability>();
            foreach (var capability in capabilities)
            {
                if (!result.Contains(capability))
                {
                    result.Add(capability);
                }
            }

            if (result.Count == 0)
            {
                throw new ArgumentException("At least one capability is required.", nameof(capabilities));
            }

            return result.AsReadOnly();
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
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

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }

        private static long RequireNonNegative(long value, string parameterName)
        {
            if (value < 0L)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }
    }
}
