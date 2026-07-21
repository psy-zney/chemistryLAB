using System;

namespace ChemistryLab.Domain
{
    public enum ChemicalState
    {
        Solid = 0,
        Liquid = 1,
        Gas = 2,
        Aqueous = 3
    }

    public enum SolubilityLevel
    {
        Insoluble = 0,
        SlightlySoluble = 1,
        ModeratelySoluble = 2,
        Soluble = 3,
        Miscible = 4,
        Immiscible = 5
    }

    /// <summary>
    /// Gameplay-only risk label. It is not a replacement for a real Safety Data Sheet.
    /// </summary>
    public enum HazardTier
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>
    /// A reviewed, static entry in the in-game chemical catalogue.
    /// </summary>
    public sealed class ChemicalItem
    {
        public ChemicalItem(
            string id,
            string nameKey,
            string formula,
            ElementGroup group,
            ChemicalState state,
            string color,
            string odorKey,
            SolubilityLevel solubility,
            HazardTier hazardTier,
            ItemRarity rarity,
            long price,
            string iconAddress,
            string sourceRef,
            string reviewer,
            DateTimeOffset reviewedAt,
            string contentVersion,
            int unlockLevel = 0)
        {
            Id = RequireText(id, nameof(id));
            NameKey = RequireText(nameKey, nameof(nameKey));
            Formula = RequireText(formula, nameof(formula));
            Group = group;
            State = state;
            Color = RequireText(color, nameof(color));
            OdorKey = RequireText(odorKey, nameof(odorKey));
            Solubility = solubility;
            HazardTier = hazardTier;
            Rarity = rarity;
            Price = RequireNonNegative(price, nameof(price));
            IconAddress = RequireText(iconAddress, nameof(iconAddress));
            SourceRef = RequireText(sourceRef, nameof(sourceRef));
            Reviewer = RequireText(reviewer, nameof(reviewer));
            ReviewedAt = reviewedAt;
            ContentVersion = RequireText(contentVersion, nameof(contentVersion));
            UnlockLevel = RequireNonNegative(unlockLevel, nameof(unlockLevel));
        }

        public string Id { get; private set; }
        public string NameKey { get; private set; }
        public string Formula { get; private set; }
        public ElementGroup Group { get; private set; }
        public ChemicalState State { get; private set; }
        public string Color { get; private set; }
        public string OdorKey { get; private set; }
        public SolubilityLevel Solubility { get; private set; }
        public HazardTier HazardTier { get; private set; }
        public ItemRarity Rarity { get; private set; }
        public long Price { get; private set; }
        public string IconAddress { get; private set; }
        public string SourceRef { get; private set; }
        public string Reviewer { get; private set; }
        public DateTimeOffset ReviewedAt { get; private set; }
        public string ContentVersion { get; private set; }
        /// <summary>Minimum player level required to purchase this catalogue chemical.</summary>
        public int UnlockLevel { get; private set; }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }

        private static long RequireNonNegative(long value, string parameterName)
        {
            if (value < 0)
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
    }
}
