using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Application
{
    public enum ContentValidationSeverity
    {
        Error = 0,
        Warning = 1
    }

    /// <summary>
    /// A CI/editor-friendly content finding. Row identifies the catalogue record;
    /// Field identifies the exact column or relationship that needs correction.
    /// </summary>
    public sealed class ContentValidationIssue
    {
        public ContentValidationIssue(ContentValidationSeverity severity, string row, string field, string message)
        {
            Severity = severity;
            Row = row;
            Field = field;
            Message = message;
        }

        public ContentValidationSeverity Severity { get; private set; }
        public string Row { get; private set; }
        public string Field { get; private set; }
        public string Message { get; private set; }
    }

    /// <summary>
    /// Input to ContentValidator. Importers should preserve source row information
    /// in their own logs; validator rows use stable catalogue id/index identifiers.
    /// </summary>
    public sealed class ContentCatalogue
    {
        public ContentCatalogue(
            IEnumerable<ChemicalItem> chemicalItems,
            IEnumerable<Reaction> reactions,
            IEnumerable<ReactionParticipant> reactionParticipants,
            IEnumerable<ReactionObservation> reactionObservations,
            IEnumerable<LabTool> labTools,
            IEnumerable<QuestTemplate> questTemplates)
        {
            ChemicalItems = Copy(chemicalItems, nameof(chemicalItems));
            Reactions = Copy(reactions, nameof(reactions));
            ReactionParticipants = Copy(reactionParticipants, nameof(reactionParticipants));
            ReactionObservations = Copy(reactionObservations, nameof(reactionObservations));
            LabTools = Copy(labTools, nameof(labTools));
            QuestTemplates = Copy(questTemplates, nameof(questTemplates));
        }

        public IReadOnlyList<ChemicalItem> ChemicalItems { get; private set; }
        public IReadOnlyList<Reaction> Reactions { get; private set; }
        public IReadOnlyList<ReactionParticipant> ReactionParticipants { get; private set; }
        public IReadOnlyList<ReactionObservation> ReactionObservations { get; private set; }
        public IReadOnlyList<LabTool> LabTools { get; private set; }
        public IReadOnlyList<QuestTemplate> QuestTemplates { get; private set; }

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source, string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return new List<T>(source).AsReadOnly();
        }
    }

    /// <summary>
    /// Validates static gameplay content without Unity, database, or editor dependencies.
    /// Any Error finding is suitable for failing CI or an import operation.
    /// </summary>
    public sealed class ContentValidator
    {
        public IReadOnlyList<ContentValidationIssue> Validate(ContentCatalogue catalogue)
        {
            if (catalogue == null)
            {
                throw new ArgumentNullException(nameof(catalogue));
            }

            var issues = new List<ContentValidationIssue>();
            var itemIds = ValidateItems(catalogue.ChemicalItems, issues);
            var reactionIds = ValidateReactions(catalogue.Reactions, issues);
            var toolIds = ValidateTools(catalogue.LabTools, issues);
            var questIds = ValidateQuests(catalogue.QuestTemplates, reactionIds, itemIds, issues);

            ValidateParticipants(catalogue.ReactionParticipants, reactionIds, itemIds, issues);
            ValidateReactionRequirements(catalogue.Reactions, catalogue.ReactionParticipants, itemIds, toolIds, issues);
            ValidateObservations(catalogue.ReactionObservations, reactionIds, issues);
            ValidateQuestPrerequisites(catalogue.QuestTemplates, questIds, reactionIds, issues);

            return issues.AsReadOnly();
        }

        private static HashSet<string> ValidateItems(IReadOnlyList<ChemicalItem> items, List<ContentValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var row = Row("ChemicalItem", index, item == null ? null : item.Id);
                if (item == null)
                {
                    AddError(issues, row, "Record", "Record cannot be null.");
                    continue;
                }

                AddIdIssueIfNeeded(issues, ids, row, item.Id);
                AddNonNegativeIssueIfNeeded(issues, row, "Price", item.Price);
                AddNonNegativeIssueIfNeeded(issues, row, "UnlockLevel", item.UnlockLevel);
                ValidateReviewMetadata(issues, row, item.SourceRef, item.Reviewer, item.ContentVersion);
            }

            return ids;
        }

        private static HashSet<string> ValidateReactions(IReadOnlyList<Reaction> reactions, List<ContentValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < reactions.Count; index++)
            {
                var reaction = reactions[index];
                var row = Row("Reaction", index, reaction == null ? null : reaction.Id);
                if (reaction == null)
                {
                    AddError(issues, row, "Record", "Record cannot be null.");
                    continue;
                }

                AddIdIssueIfNeeded(issues, ids, row, reaction.Id);
                AddNonNegativeIssueIfNeeded(issues, row, "UnlockLevel", reaction.UnlockLevel);
                AddNonNegativeIssueIfNeeded(issues, row, "AnimationDurationSeconds", reaction.AnimationDurationSeconds);
                if (reaction.OutputMassGram <= 0m)
                {
                    AddError(issues, row, "OutputMassGram", "Output mass must be greater than zero.");
                }

                ValidateReviewMetadata(issues, row, reaction.SourceRef, reaction.Reviewer, reaction.ContentVersion);
            }

            return ids;
        }

        private static HashSet<string> ValidateTools(IReadOnlyList<LabTool> tools, List<ContentValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < tools.Count; index++)
            {
                var tool = tools[index];
                var row = Row("LabTool", index, tool == null ? null : tool.Id);
                if (tool == null)
                {
                    AddError(issues, row, "Record", "Record cannot be null.");
                    continue;
                }

                AddIdIssueIfNeeded(issues, ids, row, tool.Id);
                AddNonNegativeIssueIfNeeded(issues, row, "CapacityGram", tool.CapacityGram);
                AddNonNegativeIssueIfNeeded(issues, row, "UnlockLevel", tool.UnlockLevel);
                AddNonNegativeIssueIfNeeded(issues, row, "Price", tool.Price);
                if (tool.Capabilities == null || tool.Capabilities.Count == 0)
                {
                    AddError(issues, row, "Capabilities", "At least one capability is required.");
                }

                ValidateReviewMetadata(issues, row, tool.SourceRef, tool.Reviewer, tool.ContentVersion);
            }

            return ids;
        }

        private static HashSet<string> ValidateQuests(
            IReadOnlyList<QuestTemplate> quests,
            HashSet<string> reactionIds,
            HashSet<string> itemIds,
            List<ContentValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < quests.Count; index++)
            {
                var quest = quests[index];
                var row = Row("QuestTemplate", index, quest == null ? null : quest.Id);
                if (quest == null)
                {
                    AddError(issues, row, "Record", "Record cannot be null.");
                    continue;
                }

                AddIdIssueIfNeeded(issues, ids, row, quest.Id);
                ValidateForeignKey(issues, row, "TargetReactionId", quest.TargetReactionId, reactionIds, "Reaction");
                ValidateReviewMetadata(issues, row, quest.SourceRef, quest.Reviewer, quest.ContentVersion);

                if (quest.RewardRule == null)
                {
                    AddError(issues, row, "RewardRule", "Reward rule is required.");
                    continue;
                }

                AddNonNegativeIssueIfNeeded(issues, row, "RewardRule.Dollars", quest.RewardRule.Dollars);
                AddNonNegativeIssueIfNeeded(issues, row, "RewardRule.Experience", quest.RewardRule.Experience);
                if (quest.RewardRule.ItemUnlockIds == null)
                {
                    AddError(issues, row, "RewardRule.ItemUnlockIds", "Item unlock list cannot be null.");
                    continue;
                }

                foreach (var itemUnlockId in quest.RewardRule.ItemUnlockIds)
                {
                    ValidateForeignKey(issues, row, "RewardRule.ItemUnlockIds", itemUnlockId, itemIds, "ChemicalItem");
                }
            }

            return ids;
        }

        private static void ValidateParticipants(
            IReadOnlyList<ReactionParticipant> participants,
            HashSet<string> reactionIds,
            HashSet<string> itemIds,
            List<ContentValidationIssue> issues)
        {
            for (var index = 0; index < participants.Count; index++)
            {
                var participant = participants[index];
                var row = Row("ReactionParticipant", index, participant == null ? null : participant.ReactionId + ":" + participant.ItemId);
                if (participant == null)
                {
                    AddError(issues, row, "Record", "Record cannot be null.");
                    continue;
                }

                ValidateForeignKey(issues, row, "ReactionId", participant.ReactionId, reactionIds, "Reaction");
                ValidateForeignKey(issues, row, "ItemId", participant.ItemId, itemIds, "ChemicalItem");
                if (participant.Coefficient <= 0)
                {
                    AddError(issues, row, "Coefficient", "Coefficient must be greater than zero.");
                }

                if (participant.MassGramGame <= 0m)
                {
                    AddError(issues, row, "MassGramGame", "Mass must be greater than zero.");
                }

                AddNonNegativeIssueIfNeeded(issues, row, "PourOrder", participant.PourOrder);
            }
        }

        private static void ValidateReactionRequirements(
            IReadOnlyList<Reaction> reactions,
            IReadOnlyList<ReactionParticipant> participants,
            HashSet<string> itemIds,
            HashSet<string> toolIds,
            List<ContentValidationIssue> issues)
        {
            for (var reactionIndex = 0; reactionIndex < reactions.Count; reactionIndex++)
            {
                var reaction = reactions[reactionIndex];
                if (reaction == null)
                {
                    continue;
                }

                var row = Row("Reaction", reactionIndex, reaction.Id);
                var hasReactant = false;
                var hasProduct = false;
                for (var participantIndex = 0; participantIndex < participants.Count; participantIndex++)
                {
                    var participant = participants[participantIndex];
                    if (participant == null || !string.Equals(participant.ReactionId, reaction.Id, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    hasReactant |= participant.Role == ReactionParticipantRole.Reactant;
                    hasProduct |= participant.Role == ReactionParticipantRole.Product;
                }

                if (!hasReactant)
                {
                    AddError(issues, row, "Participants", "Reaction must have at least one reactant.");
                }

                if (!hasProduct)
                {
                    AddError(issues, row, "Participants", "Reaction must have at least one product.");
                }

                ValidateForeignKey(issues, row, "OutputId", reaction.OutputId, itemIds, "ChemicalItem");
                ValidateForeignKey(issues, row, "ToolId", reaction.ToolId, toolIds, "LabTool");
            }
        }

        private static void ValidateObservations(IReadOnlyList<ReactionObservation> observations, HashSet<string> reactionIds, List<ContentValidationIssue> issues)
        {
            var sequences = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < observations.Count; index++)
            {
                var observation = observations[index];
                var row = Row("ReactionObservation", index, observation == null ? null : observation.ReactionId + ":" + observation.Sequence);
                if (observation == null)
                {
                    AddError(issues, row, "Record", "Record cannot be null.");
                    continue;
                }

                ValidateForeignKey(issues, row, "ReactionId", observation.ReactionId, reactionIds, "Reaction");
                var sequenceKey = observation.ReactionId + "|" + observation.Sequence;
                if (!sequences.Add(sequenceKey))
                {
                    AddError(issues, row, "Sequence", "Observation sequence must be unique within a reaction.");
                }

                AddNonNegativeIssueIfNeeded(issues, row, "Sequence", observation.Sequence);
                if (string.IsNullOrWhiteSpace(observation.LocalisationKey))
                {
                    AddError(issues, row, "LocalisationKey", "A localised observation description is required.");
                }

                if (string.IsNullOrWhiteSpace(observation.ParticleAssetKey)
                    && string.IsNullOrWhiteSpace(observation.AudioAssetKey)
                    && string.IsNullOrWhiteSpace(observation.HapticAssetKey))
                {
                    issues.Add(new ContentValidationIssue(
                        ContentValidationSeverity.Warning,
                        row,
                        "AssetKeys",
                        "No particle, audio, or haptic asset is assigned; only the text/UI fallback will be shown."));
                }

                ValidateReviewMetadata(issues, row, observation.SourceRef, observation.Reviewer, observation.ContentVersion);
            }
        }

        private static void ValidateQuestPrerequisites(
            IReadOnlyList<QuestTemplate> quests,
            HashSet<string> questIds,
            HashSet<string> reactionIds,
            List<ContentValidationIssue> issues)
        {
            for (var index = 0; index < quests.Count; index++)
            {
                var quest = quests[index];
                if (quest == null || quest.Prerequisites == null)
                {
                    continue;
                }

                var row = Row("QuestTemplate", index, quest.Id);
                foreach (var prerequisite in quest.Prerequisites)
                {
                    if (prerequisite == null)
                    {
                        AddError(issues, row, "Prerequisites", "Prerequisite cannot be null.");
                        continue;
                    }

                    if (prerequisite.RequiredAmount <= 0)
                    {
                        AddError(issues, row, "Prerequisites.RequiredAmount", "Required amount must be greater than zero.");
                    }

                    if (prerequisite.Type == QuestPrerequisiteType.QuestCompleted)
                    {
                        ValidateForeignKey(issues, row, "Prerequisites.TargetId", prerequisite.TargetId, questIds, "QuestTemplate");
                    }
                    else if (prerequisite.Type == QuestPrerequisiteType.ReactionDiscovered)
                    {
                        ValidateForeignKey(issues, row, "Prerequisites.TargetId", prerequisite.TargetId, reactionIds, "Reaction");
                    }
                }
            }
        }

        private static void ValidateReviewMetadata(List<ContentValidationIssue> issues, string row, string sourceRef, string reviewer, string contentVersion)
        {
            RequireText(issues, row, "SourceRef", sourceRef);
            RequireText(issues, row, "Reviewer", reviewer);
            RequireText(issues, row, "ContentVersion", contentVersion);
        }

        private static void RequireText(List<ContentValidationIssue> issues, string row, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(issues, row, field, "A non-empty value is required.");
            }
        }

        private static void ValidateForeignKey(
            List<ContentValidationIssue> issues,
            string row,
            string field,
            string value,
            HashSet<string> validIds,
            string targetType,
            string unavailableLookupMessage = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(issues, row, field, "A non-empty " + targetType + " id is required.");
                return;
            }

            if (validIds == null)
            {
                AddError(issues, row, field, unavailableLookupMessage ?? "Lookup is unavailable.");
                return;
            }

            if (!validIds.Contains(value))
            {
                AddError(issues, row, field, "Referenced " + targetType + " id '" + value + "' does not exist.");
            }
        }

        private static void AddIdIssueIfNeeded(List<ContentValidationIssue> issues, HashSet<string> ids, string row, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                AddError(issues, row, "Id", "A non-empty id is required.");
            }
            else if (!ids.Add(id))
            {
                AddError(issues, row, "Id", "Duplicate id '" + id + "'.");
            }
        }

        private static void AddNonNegativeIssueIfNeeded(List<ContentValidationIssue> issues, string row, string field, long value)
        {
            if (value < 0L)
            {
                AddError(issues, row, field, "Value cannot be negative.");
            }
        }

        private static void AddNonNegativeIssueIfNeeded(List<ContentValidationIssue> issues, string row, string field, int value)
        {
            if (value < 0)
            {
                AddError(issues, row, field, "Value cannot be negative.");
            }
        }

        private static void AddNonNegativeIssueIfNeeded(List<ContentValidationIssue> issues, string row, string field, decimal value)
        {
            if (value < 0m)
            {
                AddError(issues, row, field, "Value cannot be negative.");
            }
        }

        private static void AddError(List<ContentValidationIssue> issues, string row, string field, string message)
        {
            issues.Add(new ContentValidationIssue(ContentValidationSeverity.Error, row, field, message));
        }

        private static string Row(string type, int index, string id)
        {
            return string.IsNullOrWhiteSpace(id) ? type + "[" + index + "]" : type + "[" + id + "]";
        }
    }
}
