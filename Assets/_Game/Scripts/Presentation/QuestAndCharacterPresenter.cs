using System;
using System.Collections.Generic;
using ChemistryLab.Application;
using ChemistryLab.Domain;
using ChemistryLab.Infrastructure;

namespace ChemistryLab.Presentation
{
    public sealed class CharacterSummary
    {
        internal CharacterSummary(int level, int exp, long dollars, long diamonds, string title)
        {
            Level = level;
            Exp = exp;
            Dollars = dollars;
            Diamonds = diamonds;
            Title = title;
        }

        public int Level { get; private set; }
        public int Exp { get; private set; }
        public long Dollars { get; private set; }
        public long Diamonds { get; private set; }
        public string Title { get; private set; }
    }

    public sealed class QuestClaimPresentationResult
    {
        internal QuestClaimPresentationResult(QuestDeliveryStatus status, string localisationKey, string message)
        {
            Status = status;
            LocalisationKey = localisationKey;
            Message = message;
        }

        public QuestDeliveryStatus Status { get; private set; }
        public string LocalisationKey { get; private set; }
        public string Message { get; private set; }
        public bool IsSuccess { get { return Status == QuestDeliveryStatus.Delivered; } }
    }

    /// <summary>Presentation facade for the active quest list and character HUD/profile.</summary>
    public sealed class QuestAndCharacterPresenter
    {
        private readonly object syncRoot = new object();
        private readonly ContentCatalogue catalogue;
        private readonly ISaveRepository saveRepository;
        private readonly QuestService questService;
        private readonly Dictionary<string, QuestInstance> quests = new Dictionary<string, QuestInstance>(StringComparer.Ordinal);
        private readonly Func<PlayerProfile, string> titleResolver;

        public QuestAndCharacterPresenter(ContentCatalogue catalogue, ISaveRepository saveRepository, IEnumerable<QuestInstance> activeQuests)
            : this(catalogue, saveRepository, activeQuests, null, null)
        {
        }

        public QuestAndCharacterPresenter(
            ContentCatalogue catalogue,
            ISaveRepository saveRepository,
            IEnumerable<QuestInstance> activeQuests,
            QuestService questService,
            Func<PlayerProfile, string> titleResolver)
        {
            if (activeQuests == null) throw new ArgumentNullException(nameof(activeQuests));
            this.catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            this.saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            this.questService = questService ?? new QuestService();
            this.titleResolver = titleResolver ?? DefaultTitle;
            foreach (var quest in activeQuests)
            {
                if (quest == null) throw new ArgumentException("Active quest entries cannot be null.", nameof(activeQuests));
                if (quests.ContainsKey(quest.InstanceId)) throw new ArgumentException("Active quest instance ids must be unique.", nameof(activeQuests));
                quests.Add(quest.InstanceId, quest);
            }
        }

        public IReadOnlyList<QuestInstance> GetActiveQuests()
        {
            lock (syncRoot)
            {
                var result = new List<QuestInstance>();
                foreach (var quest in quests.Values)
                {
                    quest.ExpireIfDue(DateTimeOffset.UtcNow);
                    if (quest.Status == QuestStatus.Active || quest.Status == QuestStatus.Completed) result.Add(quest);
                }
                return result.AsReadOnly();
            }
        }

        public QuestClaimPresentationResult ClaimQuestReward(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return Failure(QuestDeliveryStatus.InvalidQuestState, "quest.delivery.invalid_state", "A quest id is required.");
            lock (syncRoot)
            {
                var instance = FindQuest(questId);
                var template = instance == null ? null : FindTemplate(instance.QuestTemplateId);
                if (instance == null || template == null) return Failure(QuestDeliveryStatus.InvalidQuestState, "quest.delivery.invalid_state", "The quest is unavailable.");

                QuestDeliveryResult delivery = null;
                try
                {
                    var transaction = CreateTransaction(instance, template);
                    var committed = saveRepository.CommitTransaction(transaction, delegate
                    {
                        delivery = questService.ValidateAndDeliver(instance, template, saveRepository.Inventory, saveRepository.Profile);
                        if (delivery.Status != QuestDeliveryStatus.Delivered) throw new QuestDeliveryException(delivery);
                    });
                    return committed
                        ? Success(delivery)
                        : Failure(QuestDeliveryStatus.AlreadyClaimed, "quest.delivery.already_claimed", "This quest reward was already claimed.");
                }
                catch (QuestDeliveryException exception) { return FromDelivery(exception.Delivery); }
                catch (Exception) { return Failure(QuestDeliveryStatus.InvalidQuestState, "quest.delivery.invalid_state", "The quest reward could not be committed."); }
            }
        }

        public CharacterSummary GetCharacterSummary()
        {
            var profile = saveRepository.LoadProfile();
            if (profile == null) return new CharacterSummary(0, 0, 0L, 0L, string.Empty);
            return new CharacterSummary(profile.Level, profile.Exp, profile.Dollars, profile.Diamonds, titleResolver(profile) ?? string.Empty);
        }

        private QuestInstance FindQuest(string questId)
        {
            QuestInstance exact;
            if (quests.TryGetValue(questId, out exact)) return exact;
            foreach (var quest in quests.Values) if (string.Equals(quest.QuestId, questId, StringComparison.Ordinal)) return quest;
            return null;
        }

        private QuestTemplate FindTemplate(string templateId)
        {
            for (var index = 0; index < catalogue.QuestTemplates.Count; index++)
                if (catalogue.QuestTemplates[index] != null && string.Equals(catalogue.QuestTemplates[index].Id, templateId, StringComparison.Ordinal)) return catalogue.QuestTemplates[index];
            return null;
        }

        private static ReactionTransaction CreateTransaction(QuestInstance instance, QuestTemplate template)
        {
            var id = Guid.NewGuid().ToString("N");
            var inputs = new Dictionary<string, decimal>(StringComparer.Ordinal) { { template.TargetReactionId, 1m } };
            return new ReactionTransaction(id, ReactionTransactionKind.QuestReward, inputs, new Dictionary<string, decimal>(StringComparer.Ordinal),
                template.RewardRule.Dollars, 0L, "quest-claim:" + instance.InstanceId + ":" + id, DateTimeOffset.UtcNow);
        }

        private static string DefaultTitle(PlayerProfile profile) { return profile.Level >= 10 ? "Senior Chemist" : "Chemist Apprentice"; }
        private static QuestClaimPresentationResult Success(QuestDeliveryResult delivery) { return FromDelivery(delivery); }
        private static QuestClaimPresentationResult FromDelivery(QuestDeliveryResult delivery) { return new QuestClaimPresentationResult(delivery.Status, delivery.LocalisationKey, delivery.Message); }
        private static QuestClaimPresentationResult Failure(QuestDeliveryStatus status, string key, string message) { return new QuestClaimPresentationResult(status, key, message); }

        private sealed class QuestDeliveryException : Exception
        {
            public QuestDeliveryException(QuestDeliveryResult delivery) { Delivery = delivery; }
            public QuestDeliveryResult Delivery { get; private set; }
        }
    }
}
