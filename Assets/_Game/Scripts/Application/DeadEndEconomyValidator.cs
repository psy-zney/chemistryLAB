using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Application
{
    /// <summary>
    /// Checks that every ingredient needed by an unlocked reaction or quest can be
    /// bought or crafted from ingredients available at that point in progression.
    /// This is deliberately a content check: it makes no assumptions about a
    /// player's current inventory or currency balance.
    /// </summary>
    public sealed class DeadEndEconomyValidator
    {
        public bool ValidateEconomy(ContentCatalogue catalogue, out List<string> errors)
        {
            errors = new List<string>();
            if (catalogue == null)
            {
                errors.Add("Content catalogue is required.");
                return false;
            }

            var items = IndexItems(catalogue.ChemicalItems, errors);
            var reactions = IndexReactions(catalogue.Reactions, errors);
            var inputsByReaction = BuildInputs(catalogue.ReactionParticipants, errors);

            ValidateReactionInputs(catalogue.Reactions, inputsByReaction, items, reactions, catalogue.LabTools, errors);
            ValidateQuestInputs(catalogue.QuestTemplates, reactions, inputsByReaction, items, catalogue.LabTools, errors);
            return errors.Count == 0;
        }

        private static Dictionary<string, ChemicalItem> IndexItems(IReadOnlyList<ChemicalItem> values, List<string> errors)
        {
            var result = new Dictionary<string, ChemicalItem>(StringComparer.Ordinal);
            if (values == null)
            {
                errors.Add("Chemical item catalogue is missing.");
                return result;
            }

            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                if (value == null || string.IsNullOrWhiteSpace(value.Id))
                {
                    errors.Add("ChemicalItem[" + index + "] is invalid.");
                }
                else if (!result.ContainsKey(value.Id))
                {
                    result.Add(value.Id, value);
                }
            }

            return result;
        }

        private static Dictionary<string, Reaction> IndexReactions(IReadOnlyList<Reaction> values, List<string> errors)
        {
            var result = new Dictionary<string, Reaction>(StringComparer.Ordinal);
            if (values == null)
            {
                errors.Add("Reaction catalogue is missing.");
                return result;
            }

            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                if (value == null || string.IsNullOrWhiteSpace(value.Id))
                {
                    errors.Add("Reaction[" + index + "] is invalid.");
                }
                else if (!result.ContainsKey(value.Id))
                {
                    result.Add(value.Id, value);
                }
            }

            return result;
        }

        private static Dictionary<string, List<string>> BuildInputs(IReadOnlyList<ReactionParticipant> values, List<string> errors)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (values == null)
            {
                errors.Add("Reaction participant catalogue is missing.");
                return result;
            }

            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                if (value == null || value.Role != ReactionParticipantRole.Reactant)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value.ReactionId) || string.IsNullOrWhiteSpace(value.ItemId))
                {
                    errors.Add("ReactionParticipant[" + index + "] has no reaction or chemical id.");
                    continue;
                }

                List<string> inputs;
                if (!result.TryGetValue(value.ReactionId, out inputs))
                {
                    inputs = new List<string>();
                    result.Add(value.ReactionId, inputs);
                }

                if (!inputs.Contains(value.ItemId))
                {
                    inputs.Add(value.ItemId);
                }
            }

            return result;
        }

        private static void ValidateReactionInputs(
            IReadOnlyList<Reaction> reactions,
            Dictionary<string, List<string>> inputsByReaction,
            Dictionary<string, ChemicalItem> items,
            Dictionary<string, Reaction> reactionsById,
            IReadOnlyList<LabTool> tools,
            List<string> errors)
        {
            if (reactions == null)
            {
                return;
            }

            for (var index = 0; index < reactions.Count; index++)
            {
                var reaction = reactions[index];
                if (reaction == null)
                {
                    continue;
                }

                ValidateInputsForConsumer(
                    "Reaction '" + reaction.Id + "'",
                    reaction.Id,
                    RequiredLevel(reaction, tools),
                    inputsByReaction,
                    items,
                    reactionsById,
                    tools,
                    errors);
            }
        }

        private static void ValidateQuestInputs(
            IReadOnlyList<QuestTemplate> quests,
            Dictionary<string, Reaction> reactions,
            Dictionary<string, List<string>> inputsByReaction,
            Dictionary<string, ChemicalItem> items,
            IReadOnlyList<LabTool> tools,
            List<string> errors)
        {
            if (quests == null)
            {
                return;
            }

            for (var index = 0; index < quests.Count; index++)
            {
                var quest = quests[index];
                if (quest == null)
                {
                    continue;
                }

                Reaction reaction;
                if (!reactions.TryGetValue(quest.TargetReactionId, out reaction))
                {
                    errors.Add("Quest '" + quest.Id + "' references unavailable reaction '" + quest.TargetReactionId + "'.");
                    continue;
                }

                ValidateInputsForConsumer(
                    "Quest '" + quest.Id + "'",
                    reaction.Id,
                    Math.Max(RequiredLevel(reaction, tools), GetQuestLevel(quest)),
                    inputsByReaction,
                    items,
                    reactions,
                    tools,
                    errors);
            }
        }

        private static void ValidateInputsForConsumer(
            string consumer,
            string reactionId,
            int level,
            Dictionary<string, List<string>> inputsByReaction,
            Dictionary<string, ChemicalItem> items,
            Dictionary<string, Reaction> reactions,
            IReadOnlyList<LabTool> tools,
            List<string> errors)
        {
            List<string> inputs;
            if (!inputsByReaction.TryGetValue(reactionId, out inputs))
            {
                errors.Add(consumer + " has no reactant list.");
                return;
            }

            for (var inputIndex = 0; inputIndex < inputs.Count; inputIndex++)
            {
                var itemId = inputs[inputIndex];
                if (!CanSupply(itemId, level, items, reactions, inputsByReaction, tools, new HashSet<string>(StringComparer.Ordinal)))
                {
                    errors.Add(
                        "Dead-end chemical '" + itemId + "' required by " + consumer + " at level " + level
                        + ": no shop source or craft path is unlocked by that level.");
                }
            }
        }

        private static bool CanSupply(
            string itemId,
            int level,
            Dictionary<string, ChemicalItem> items,
            Dictionary<string, Reaction> reactions,
            Dictionary<string, List<string>> inputsByReaction,
            IReadOnlyList<LabTool> tools,
            HashSet<string> path)
        {
            ChemicalItem item;
            if (!items.TryGetValue(itemId, out item))
            {
                return false;
            }

            // Every ChemicalItem is a shop entry; UnlockLevel is therefore its shop gate.
            if (item.UnlockLevel <= level)
            {
                return true;
            }

            if (!path.Add(itemId))
            {
                return false;
            }

            try
            {
                foreach (var reaction in reactions.Values)
                {
                    if (!string.Equals(reaction.OutputId, itemId, StringComparison.Ordinal)
                        || RequiredLevel(reaction, tools) > level)
                    {
                        continue;
                    }

                    List<string> inputs;
                    if (!inputsByReaction.TryGetValue(reaction.Id, out inputs) || inputs.Count == 0)
                    {
                        continue;
                    }

                    var allInputsAvailable = true;
                    for (var index = 0; index < inputs.Count; index++)
                    {
                        if (!CanSupply(inputs[index], level, items, reactions, inputsByReaction, tools, path))
                        {
                            allInputsAvailable = false;
                            break;
                        }
                    }

                    if (allInputsAvailable)
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                path.Remove(itemId);
            }
        }

        private static int RequiredLevel(Reaction reaction, IReadOnlyList<LabTool> tools)
        {
            var requiredLevel = reaction.UnlockLevel;
            if (tools == null)
            {
                return requiredLevel;
            }

            for (var index = 0; index < tools.Count; index++)
            {
                var tool = tools[index];
                if (tool != null && string.Equals(tool.Id, reaction.ToolId, StringComparison.Ordinal))
                {
                    return Math.Max(requiredLevel, tool.UnlockLevel);
                }
            }

            return requiredLevel;
        }

        private static int GetQuestLevel(QuestTemplate quest)
        {
            var level = 1;
            if (quest.Prerequisites == null)
            {
                return level;
            }

            for (var index = 0; index < quest.Prerequisites.Count; index++)
            {
                var prerequisite = quest.Prerequisites[index];
                if (prerequisite != null && prerequisite.Type == QuestPrerequisiteType.PlayerLevel)
                {
                    level = Math.Max(level, prerequisite.RequiredAmount);
                }
            }

            return level;
        }
    }
}
