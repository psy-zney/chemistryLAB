using System;
using System.Collections.Generic;

namespace ChemistryLab.Domain
{
    public enum QuestTier
    {
        Tutorial = 0,
        Main = 1,
        Side = 2,
        Advanced = 3
    }

    public enum QuestPrerequisiteType
    {
        QuestCompleted = 0,
        ReactionDiscovered = 1,
        PlayerLevel = 2
    }

    public sealed class QuestPrerequisite
    {
        public QuestPrerequisite(QuestPrerequisiteType type, string targetId, int requiredAmount)
        {
            Type = type;
            TargetId = string.IsNullOrWhiteSpace(targetId) ? null : targetId;
            RequiredAmount = RequirePositive(requiredAmount, nameof(requiredAmount));

            if (type != QuestPrerequisiteType.PlayerLevel && TargetId == null)
            {
                throw new ArgumentException("A target id is required for this prerequisite type.", nameof(targetId));
            }
        }

        public QuestPrerequisiteType Type { get; private set; }
        public string TargetId { get; private set; }
        public int RequiredAmount { get; private set; }

        private static int RequirePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }
    }

    public sealed class QuestRewardRule
    {
        public QuestRewardRule(long dollars, int experience, IEnumerable<string> itemUnlockIds)
        {
            Dollars = RequireNonNegative(dollars, nameof(dollars));
            Experience = RequireNonNegative(experience, nameof(experience));
            ItemUnlockIds = CopyIds(itemUnlockIds);
        }

        public long Dollars { get; private set; }
        public int Experience { get; private set; }
        public IReadOnlyList<string> ItemUnlockIds { get; private set; }

        private static IReadOnlyList<string> CopyIds(IEnumerable<string> itemUnlockIds)
        {
            if (itemUnlockIds == null)
            {
                throw new ArgumentNullException(nameof(itemUnlockIds));
            }

            var result = new List<string>();
            foreach (var id in itemUnlockIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("An item unlock id cannot be empty.", nameof(itemUnlockIds));
                }

                if (!result.Contains(id))
                {
                    result.Add(id);
                }
            }

            return result.AsReadOnly();
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
    }

    /// <summary>
    /// Reviewed NPC quest content. The template is immutable; player progress belongs in QuestInstance.
    /// </summary>
    public sealed class QuestTemplate
    {
        public QuestTemplate(
            string id,
            QuestTier tier,
            string targetReactionId,
            string dialogueKey,
            string applicationKey,
            QuestRewardRule rewardRule,
            IEnumerable<QuestPrerequisite> prerequisites,
            string sourceRef,
            string reviewer,
            DateTimeOffset reviewedAt,
            string contentVersion)
        {
            Id = RequireText(id, nameof(id));
            Tier = tier;
            TargetReactionId = RequireText(targetReactionId, nameof(targetReactionId));
            DialogueKey = RequireText(dialogueKey, nameof(dialogueKey));
            ApplicationKey = RequireText(applicationKey, nameof(applicationKey));
            RewardRule = rewardRule ?? throw new ArgumentNullException(nameof(rewardRule));
            Prerequisites = CopyPrerequisites(prerequisites);
            SourceRef = RequireText(sourceRef, nameof(sourceRef));
            Reviewer = RequireText(reviewer, nameof(reviewer));
            ReviewedAt = reviewedAt;
            ContentVersion = RequireText(contentVersion, nameof(contentVersion));
        }

        public string Id { get; private set; }
        public QuestTier Tier { get; private set; }
        public string TargetReactionId { get; private set; }
        public string DialogueKey { get; private set; }
        public string ApplicationKey { get; private set; }
        public QuestRewardRule RewardRule { get; private set; }
        public IReadOnlyList<QuestPrerequisite> Prerequisites { get; private set; }
        public string SourceRef { get; private set; }
        public string Reviewer { get; private set; }
        public DateTimeOffset ReviewedAt { get; private set; }
        public string ContentVersion { get; private set; }

        private static IReadOnlyList<QuestPrerequisite> CopyPrerequisites(IEnumerable<QuestPrerequisite> prerequisites)
        {
            if (prerequisites == null)
            {
                throw new ArgumentNullException(nameof(prerequisites));
            }

            var result = new List<QuestPrerequisite>();
            foreach (var prerequisite in prerequisites)
            {
                if (prerequisite == null)
                {
                    throw new ArgumentException("A prerequisite cannot be null.", nameof(prerequisites));
                }

                result.Add(prerequisite);
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
    }
}
