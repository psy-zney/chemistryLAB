using System;

namespace ChemistryLab.Domain
{
    public enum QuestStatus
    {
        Active = 0,
        Completed = 1,
        Claimed = 2,
        Expired = 3
    }

    public enum QuestClaimResult
    {
        Claimed = 0,
        AlreadyClaimed = 1,
        DuplicateClaim = 2,
        NotCompleted = 3,
        Expired = 4
    }

    /// <summary>
    /// Player-specific quest state. A repeated claim request with the same claim id
    /// is safe and returns AlreadyClaimed without granting the reward again.
    /// </summary>
    public sealed class QuestInstance
    {
        public QuestInstance(string instanceId, string questTemplateId, int deterministicSeed, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
        {
            InstanceId = RequireText(instanceId, nameof(instanceId));
            QuestTemplateId = RequireText(questTemplateId, nameof(questTemplateId));
            DeterministicSeed = deterministicSeed;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;

            if (expiresAt.HasValue && expiresAt.Value < createdAt)
            {
                throw new ArgumentOutOfRangeException(nameof(expiresAt), "Expiry cannot be earlier than creation.");
            }

            Status = QuestStatus.Active;
        }

        public string InstanceId { get; private set; }
        public string QuestTemplateId { get; private set; }
        public string QuestId { get { return QuestTemplateId; } }
        public int DeterministicSeed { get; private set; }
        public QuestStatus Status { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? ExpiresAt { get; private set; }
        public string ClaimId { get; private set; }

        public bool TryComplete(DateTimeOffset now)
        {
            ExpireIfDue(now);
            if (Status != QuestStatus.Active)
            {
                return false;
            }

            Status = QuestStatus.Completed;
            return true;
        }

        public QuestClaimResult TryClaim(string claimId, DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(claimId))
            {
                throw new ArgumentException("A non-empty value is required.", nameof(claimId));
            }

            ExpireIfDue(now);
            if (Status == QuestStatus.Expired)
            {
                return QuestClaimResult.Expired;
            }

            if (Status == QuestStatus.Claimed)
            {
                return string.Equals(ClaimId, claimId, StringComparison.Ordinal)
                    ? QuestClaimResult.AlreadyClaimed
                    : QuestClaimResult.DuplicateClaim;
            }

            if (Status != QuestStatus.Completed)
            {
                return QuestClaimResult.NotCompleted;
            }

            ClaimId = claimId;
            Status = QuestStatus.Claimed;
            return QuestClaimResult.Claimed;
        }

        public bool ExpireIfDue(DateTimeOffset now)
        {
            if ((Status == QuestStatus.Active || Status == QuestStatus.Completed)
                && ExpiresAt.HasValue
                && now >= ExpiresAt.Value)
            {
                Status = QuestStatus.Expired;
                return true;
            }

            return false;
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
