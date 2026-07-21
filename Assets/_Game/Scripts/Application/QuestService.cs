using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Application
{
    public enum QuestDeliveryStatus
    {
        Delivered = 0,
        AlreadyClaimed = 1,
        TemplateMismatch = 2,
        MissingRequiredProduct = 3,
        InvalidQuestState = 4,
        RewardOverflow = 5
    }

    /// <summary>Outcome of an atomic inventory delivery and reward claim.</summary>
    public sealed class QuestDeliveryResult
    {
        internal QuestDeliveryResult(QuestDeliveryStatus status, string message, string localisationKey, ReactionTransaction transaction)
        {
            Status = status;
            Message = message;
            LocalisationKey = localisationKey;
            Transaction = transaction;
        }

        public QuestDeliveryStatus Status { get; private set; }
        public string Message { get; private set; }
        public string LocalisationKey { get; private set; }
        public ReactionTransaction Transaction { get; private set; }
    }

    /// <summary>
    /// Coordinates deterministic quest creation and one-time delivery. Quest content
    /// currently identifies the requested delivery item with TargetReactionId; content
    /// should use the same id for the collected product until a separate target-item
    /// field is added to QuestTemplate.
    /// </summary>
    public sealed class QuestService
    {
        private readonly object syncRoot = new object();

        public QuestInstance GenerateQuestInstance(QuestTemplate template, int seed)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            // The stable id makes regenerating the same template/seed idempotent for
            // persistence layers while CreatedAt remains the time it was offered.
            var instanceId = template.Id + ":" + seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return new QuestInstance(instanceId, template.Id, seed, DateTimeOffset.UtcNow, null);
        }

        public QuestDeliveryResult ValidateAndDeliver(
            QuestInstance instance,
            QuestTemplate template,
            Inventory inventory,
            PlayerProfile profile)
        {
            if (instance == null || template == null || inventory == null || profile == null)
            {
                return Failure(QuestDeliveryStatus.InvalidQuestState, "A quest instance, template, inventory, and profile are required.", "quest.delivery.invalid_state");
            }

            if (!string.Equals(instance.QuestTemplateId, template.Id, StringComparison.Ordinal))
            {
                return Failure(QuestDeliveryStatus.TemplateMismatch, "The quest instance does not belong to this template.", "quest.delivery.template_mismatch");
            }

            lock (syncRoot)
            {
                if (instance.Status == QuestStatus.Claimed)
                {
                    return Failure(QuestDeliveryStatus.AlreadyClaimed, "This quest reward was already claimed.", "quest.delivery.already_claimed");
                }

                if (instance.Status != QuestStatus.Active)
                {
                    return Failure(QuestDeliveryStatus.InvalidQuestState, "This quest is not available for delivery.", "quest.delivery.invalid_state");
                }

                var deliveryItemId = template.TargetReactionId;
                const decimal deliveryGram = 1m;
                if (inventory.GetGram(deliveryItemId) < deliveryGram)
                {
                    return Failure(QuestDeliveryStatus.MissingRequiredProduct, "The required quest product is not in inventory.", "quest.delivery.missing_product");
                }

                if (WouldOverflow(profile.Dollars, template.RewardRule.Dollars)
                    || WouldOverflow(profile.Exp, template.RewardRule.Experience))
                {
                    return Failure(QuestDeliveryStatus.RewardOverflow, "The quest reward cannot be applied safely.", "quest.delivery.reward_overflow");
                }

                var now = DateTimeOffset.UtcNow;
                if (!instance.TryComplete(now))
                {
                    return Failure(QuestDeliveryStatus.InvalidQuestState, "The quest could not be completed.", "quest.delivery.invalid_state");
                }

                var claimId = "delivery:" + instance.InstanceId;
                if (instance.TryClaim(claimId, now) != QuestClaimResult.Claimed)
                {
                    // With a single lock and an active instance this is unreachable;
                    // do not mutate inventory/profile if the claim cannot be recorded.
                    return Failure(QuestDeliveryStatus.InvalidQuestState, "The quest reward could not be claimed.", "quest.delivery.invalid_state");
                }

                // Preconditions above ensure every mutation is valid. These changes are
                // intentionally adjacent for a single persistence transaction at the edge.
                inventory.RemoveGram(deliveryItemId, deliveryGram);
                if (template.RewardRule.Dollars > 0L)
                {
                    profile.AddDollars(template.RewardRule.Dollars);
                }

                if (template.RewardRule.Experience > 0)
                {
                    profile.AddExp(template.RewardRule.Experience);
                }

                var transactionId = Guid.NewGuid().ToString("N");
                var inputs = new Dictionary<string, decimal>(StringComparer.Ordinal)
                {
                    { deliveryItemId, deliveryGram }
                };
                var transaction = new ReactionTransaction(
                    transactionId,
                    ReactionTransactionKind.QuestReward,
                    inputs,
                    new Dictionary<string, decimal>(StringComparer.Ordinal),
                    template.RewardRule.Dollars,
                    0L,
                    "quest-delivery:" + instance.InstanceId,
                    now);

                return new QuestDeliveryResult(QuestDeliveryStatus.Delivered, "Quest delivered and reward claimed.", "quest.delivery.success", transaction);
            }
        }

        private static bool WouldOverflow(long current, long amount)
        {
            return amount > 0L && current > long.MaxValue - amount;
        }

        private static bool WouldOverflow(int current, int amount)
        {
            return amount > 0 && current > int.MaxValue - amount;
        }

        private static QuestDeliveryResult Failure(QuestDeliveryStatus status, string message, string localisationKey)
        {
            return new QuestDeliveryResult(status, message, localisationKey, null);
        }
    }
}
