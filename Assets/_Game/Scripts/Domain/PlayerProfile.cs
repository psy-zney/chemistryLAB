using System;

namespace ChemistryLab.Domain
{
    /// <summary>
    /// Player-owned progression and currency state. Persistence and UI should use
    /// application services rather than changing these values directly.
    /// </summary>
    public sealed class PlayerProfile
    {
        /// <summary>Fixed progression curve until content supplies a level table.</summary>
        public const int ExperiencePerLevel = 100;
        public PlayerProfile(
            string playerId,
            long dollars = 0L,
            long diamonds = 0L,
            int level = 1,
            int exp = 0,
            int labUpgradeLevel = 1,
            string activeLabId = null,
            AvatarData avatar = null,
            int schemaVersion = 1,
            DateTimeOffset? createdAt = null,
            DateTimeOffset? updatedAt = null)
        {
            PlayerId = RequireText(playerId, nameof(playerId));
            Dollars = RequireNonNegative(dollars, nameof(dollars));
            Diamonds = RequireNonNegative(diamonds, nameof(diamonds));
            Level = RequirePositive(level, nameof(level));
            Exp = RequireNonNegative(exp, nameof(exp));
            LabUpgradeLevel = RequirePositive(labUpgradeLevel, nameof(labUpgradeLevel));
            ActiveLabId = string.IsNullOrWhiteSpace(activeLabId) ? null : activeLabId;
            Avatar = avatar ?? new AvatarData();
            SchemaVersion = RequirePositive(schemaVersion, nameof(schemaVersion));
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
            UpdatedAt = updatedAt ?? CreatedAt;

            if (UpdatedAt < CreatedAt)
            {
                throw new ArgumentOutOfRangeException(nameof(updatedAt), "UpdatedAt cannot be before CreatedAt.");
            }
        }

        public string PlayerId { get; private set; }
        public long Dollars { get; private set; }
        public long Diamonds { get; private set; }
        public int Level { get; private set; }
        public int Exp { get; private set; }
        public int LabUpgradeLevel { get; private set; }
        public string ActiveLabId { get; private set; }
        public AvatarData Avatar { get; private set; }
        public int SchemaVersion { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }

        public void SetAvatar(AvatarData newAvatar)
        {
            Avatar = newAvatar ?? new AvatarData();
            Touch();
        }

        public void AddDollars(long amount)
        {
            Dollars = checked(Dollars + RequirePositive(amount, nameof(amount)));
            Touch();
        }

        public bool TrySpendDollars(long amount)
        {
            RequirePositive(amount, nameof(amount));
            if (Dollars < amount)
            {
                return false;
            }

            Dollars -= amount;
            Touch();
            return true;
        }

        public void AddDiamonds(long amount)
        {
            Diamonds = checked(Diamonds + RequirePositive(amount, nameof(amount)));
            Touch();
        }

        public bool TrySpendDiamonds(long amount)
        {
            RequirePositive(amount, nameof(amount));
            if (Diamonds < amount)
            {
                return false;
            }

            Diamonds -= amount;
            Touch();
            return true;
        }

        public void AddExp(int amount)
        {
            Exp = checked(Exp + RequirePositive(amount, nameof(amount)));
            while (Exp >= ExperiencePerLevel)
            {
                Exp -= ExperiencePerLevel;
                Level = checked(Level + 1);
            }
            Touch();
        }

        public void SetLevel(int level)
        {
            Level = RequirePositive(level, nameof(level));
            Touch();
        }

        public void SetLabUpgradeLevel(int level)
        {
            LabUpgradeLevel = RequirePositive(level, nameof(level));
            Touch();
        }

        public void SetActiveLab(string activeLabId)
        {
            ActiveLabId = string.IsNullOrWhiteSpace(activeLabId) ? null : activeLabId;
            Touch();
        }

        /// <summary>Updates the persisted schema marker after a successful migration step.</summary>
        public void SetSchemaVersion(int schemaVersion)
        {
            SchemaVersion = RequirePositive(schemaVersion, nameof(schemaVersion));
            Touch();
        }

        private void Touch()
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }

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
            if (value < 0L)
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

        private static long RequirePositive(long value, string parameterName)
        {
            if (value <= 0L)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }

        private static int RequirePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }
    }
}
