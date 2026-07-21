using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Application
{
    public enum ReactionResolveStatus
    {
        Success = 0,
        MissingInput = 1,
        WrongAmount = 2,
        WrongTool = 3,
        Locked = 4,
        InvalidState = 5
    }

    /// <summary>UI-safe, deterministic outcome of validating a bench against one catalogue reaction.</summary>
    public sealed class ReactionResolveResult
    {
        private ReactionResolveResult(
            ReactionResolveStatus status,
            string message,
            string localisationKey,
            string outputId,
            decimal outputMassGram,
            string reactionId,
            ReactionTransaction transaction)
        {
            Status = status;
            Message = message;
            LocalisationKey = localisationKey;
            OutputId = outputId;
            OutputMassGram = outputMassGram;
            ReactionId = reactionId;
            Transaction = transaction;
        }

        public ReactionResolveStatus Status { get; private set; }
        public string Message { get; private set; }
        public string LocalisationKey { get; private set; }
        public string OutputId { get; private set; }
        public decimal OutputMassGram { get; private set; }
        public string ReactionId { get; private set; }

        /// <summary>
        /// Present only for Success. A persistence service must apply this proposal
        /// atomically and enforce IdempotencyKey before reporting the craft complete.
        /// </summary>
        public ReactionTransaction Transaction { get; private set; }

        internal static ReactionResolveResult Failure(
            ReactionResolveStatus status,
            string message,
            string localisationKey,
            string reactionId)
        {
            return new ReactionResolveResult(status, message, localisationKey, null, 0m, reactionId, null);
        }

        internal static ReactionResolveResult Success(Reaction reaction, ReactionTransaction transaction)
        {
            return new ReactionResolveResult(
                ReactionResolveStatus.Success,
                "Reaction resolved successfully.",
                "reaction.resolve.success",
                reaction.OutputId,
                reaction.OutputMassGram,
                reaction.Id,
                transaction);
        }
    }

    /// <summary>
    /// Resolves only reviewed, catalogue-defined reactions. It never infers a
    /// chemical reaction and it does not mutate a player profile or inventory.
    /// </summary>
    public sealed class ReactionResolver
    {
        public ReactionResolveResult Resolve(
            Reaction targetReaction,
            IReadOnlyDictionary<string, decimal> benchItems,
            LabTool usedTool,
            PlayerProfile profile,
            ContentCatalogue catalogue)
        {
            var requestedReactionId = targetReaction == null ? null : targetReaction.Id;
            if (targetReaction == null || profile == null || catalogue == null || catalogue.Reactions == null)
            {
                return ReactionResolveResult.Failure(
                    ReactionResolveStatus.InvalidState,
                    "A reaction, player profile, and catalogue are required.",
                    "reaction.resolve.invalid_state",
                    requestedReactionId);
            }

            var reaction = FindReaction(catalogue.Reactions, targetReaction.Id);
            if (reaction == null)
            {
                return ReactionResolveResult.Failure(
                    ReactionResolveStatus.InvalidState,
                    "The requested reaction is not available in the catalogue.",
                    "reaction.resolve.invalid_reaction",
                    targetReaction.Id);
            }

            var requiredTool = FindTool(catalogue.LabTools, reaction.ToolId);
            if (requiredTool == null)
            {
                return ReactionResolveResult.Failure(
                    ReactionResolveStatus.InvalidState,
                    "The reaction references an unavailable tool.",
                    "reaction.resolve.invalid_catalogue",
                    reaction.Id);
            }

            var requiredLevel = Math.Max(reaction.UnlockLevel, requiredTool.UnlockLevel);
            if (profile.Level < requiredLevel || profile.LabUpgradeLevel < requiredLevel)
            {
                return ReactionResolveResult.Failure(
                    ReactionResolveStatus.Locked,
                    "The reaction is not unlocked for this player or laboratory.",
                    "reaction.resolve.locked",
                    reaction.Id);
            }

            if (usedTool == null
                || !string.Equals(usedTool.Id, reaction.ToolId, StringComparison.Ordinal)
                || !usedTool.IsOwned
                || !HasAllCapabilities(usedTool, requiredTool.Capabilities))
            {
                return ReactionResolveResult.Failure(
                    ReactionResolveStatus.WrongTool,
                    "The selected tool does not meet the reaction requirements.",
                    "reaction.resolve.wrong_tool",
                    reaction.Id);
            }

            if (!ValidateBenchItems(benchItems, reaction.Id))
            {
                return ReactionResolveResult.Failure(
                    ReactionResolveStatus.InvalidState,
                    "The bench contains an invalid item or quantity.",
                    "reaction.resolve.invalid_state",
                    reaction.Id);
            }

            var expectedInputs = GetReactantMasses(catalogue.ReactionParticipants, reaction.Id);
            if (expectedInputs == null || expectedInputs.Count == 0 || !ContainsChemical(catalogue.ChemicalItems, reaction.OutputId))
            {
                return ReactionResolveResult.Failure(
                    ReactionResolveStatus.InvalidState,
                    "The reaction content is incomplete.",
                    "reaction.resolve.invalid_catalogue",
                    reaction.Id);
            }

            foreach (var expectedInput in expectedInputs)
            {
                decimal suppliedMass;
                if (!benchItems.TryGetValue(expectedInput.Key, out suppliedMass))
                {
                    return ReactionResolveResult.Failure(
                        ReactionResolveStatus.MissingInput,
                        "A required reactant is missing from the bench.",
                        "reaction.resolve.missing_input",
                        reaction.Id);
                }

                if (suppliedMass != expectedInput.Value)
                {
                    return ReactionResolveResult.Failure(
                        ReactionResolveStatus.WrongAmount,
                        "A reactant amount does not match the catalogue recipe.",
                        "reaction.resolve.wrong_amount",
                        reaction.Id);
                }
            }

            if (benchItems.Count != expectedInputs.Count)
            {
                return ReactionResolveResult.Failure(
                    ReactionResolveStatus.InvalidState,
                    "The bench contains reactants not used by this reaction.",
                    "reaction.resolve.unexpected_input",
                    reaction.Id);
            }

            var outputs = new Dictionary<string, decimal>(StringComparer.Ordinal)
            {
                { reaction.OutputId, reaction.OutputMassGram }
            };
            var transactionId = Guid.NewGuid().ToString("N");
            var transaction = new ReactionTransaction(
                transactionId,
                ReactionTransactionKind.Craft,
                expectedInputs,
                outputs,
                0L,
                0L,
                "craft:" + reaction.Id + ":" + transactionId,
                DateTimeOffset.UtcNow);

            return ReactionResolveResult.Success(reaction, transaction);
        }

        private static Reaction FindReaction(IReadOnlyList<Reaction> reactions, string reactionId)
        {
            if (reactions == null || string.IsNullOrWhiteSpace(reactionId))
            {
                return null;
            }

            for (var index = 0; index < reactions.Count; index++)
            {
                var reaction = reactions[index];
                if (reaction != null && string.Equals(reaction.Id, reactionId, StringComparison.Ordinal))
                {
                    return reaction;
                }
            }

            return null;
        }

        private static LabTool FindTool(IReadOnlyList<LabTool> tools, string toolId)
        {
            if (tools == null || string.IsNullOrWhiteSpace(toolId))
            {
                return null;
            }

            for (var index = 0; index < tools.Count; index++)
            {
                var tool = tools[index];
                if (tool != null && string.Equals(tool.Id, toolId, StringComparison.Ordinal))
                {
                    return tool;
                }
            }

            return null;
        }

        private static bool HasAllCapabilities(LabTool tool, IReadOnlyList<LabToolCapability> requiredCapabilities)
        {
            if (tool.Capabilities == null || requiredCapabilities == null)
            {
                return false;
            }

            for (var requiredIndex = 0; requiredIndex < requiredCapabilities.Count; requiredIndex++)
            {
                var found = false;
                for (var suppliedIndex = 0; suppliedIndex < tool.Capabilities.Count; suppliedIndex++)
                {
                    if (tool.Capabilities[suppliedIndex] == requiredCapabilities[requiredIndex])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateBenchItems(
            IReadOnlyDictionary<string, decimal> benchItems,
            string reactionId)
        {
            if (benchItems == null || string.IsNullOrWhiteSpace(reactionId))
            {
                return false;
            }

            foreach (var item in benchItems)
            {
                if (string.IsNullOrWhiteSpace(item.Key) || item.Value <= 0m)
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, decimal> GetReactantMasses(
            IReadOnlyList<ReactionParticipant> participants,
            string reactionId)
        {
            if (participants == null)
            {
                return null;
            }

            var masses = new Dictionary<string, decimal>(StringComparer.Ordinal);
            for (var index = 0; index < participants.Count; index++)
            {
                var participant = participants[index];
                if (participant == null
                    || !string.Equals(participant.ReactionId, reactionId, StringComparison.Ordinal)
                    || participant.Role != ReactionParticipantRole.Reactant)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(participant.ItemId)
                    || participant.Coefficient <= 0
                    || participant.MassGramGame <= 0m)
                {
                    return null;
                }

                decimal currentMass;
                masses.TryGetValue(participant.ItemId, out currentMass);
                masses[participant.ItemId] = currentMass + participant.MassGramGame;
            }

            return masses;
        }

        private static bool ContainsChemical(IReadOnlyList<ChemicalItem> items, string itemId)
        {
            if (items == null || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item != null && string.Equals(item.Id, itemId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
