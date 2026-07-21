using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Application
{
    /// <summary>
    /// Fast, framework-free domain regression suite. Invoke <see cref="RunAll"/>
    /// from an editor test wrapper or a development command runner.
    /// </summary>
    public static class DomainUnitTests
    {
        public static void RunAll()
        {
            ResolvesEveryPublicReactionStatus();
            UpdatesCurrenciesExperienceAndLevel();
            CapsEarlyGameIncidentPenalties();
            DetectsDeadEndEconomyContent();
        }

        private static void ResolvesEveryPublicReactionStatus()
        {
            var catalogue = CreateCatalogue(2, 2);
            var resolver = new ReactionResolver();
            var reaction = catalogue.Reactions[0];
            var validProfile = new PlayerProfile("resolver-player", level: 2, labUpgradeLevel: 2);
            var validTool = catalogue.LabTools[0];

            AssertStatus(ReactionResolveStatus.Success, resolver.Resolve(reaction, Bench("water", 2m), validTool, validProfile, catalogue));
            AssertStatus(ReactionResolveStatus.MissingInput, resolver.Resolve(reaction, new Dictionary<string, decimal>(StringComparer.Ordinal), validTool, validProfile, catalogue));
            AssertStatus(ReactionResolveStatus.WrongAmount, resolver.Resolve(reaction, Bench("water", 1m), validTool, validProfile, catalogue));
            AssertStatus(ReactionResolveStatus.WrongTool, resolver.Resolve(reaction, Bench("water", 2m), CreateTool("wrong-tool", 1), validProfile, catalogue));
            AssertStatus(ReactionResolveStatus.Locked, resolver.Resolve(reaction, Bench("water", 2m), validTool, new PlayerProfile("locked-player"), catalogue));
        }

        private static void UpdatesCurrenciesExperienceAndLevel()
        {
            var profile = new PlayerProfile("profile-player", dollars: 100L, diamonds: 10L);
            profile.AddDollars(50L);
            Assert(profile.TrySpendDollars(40L), "Expected the dollar spend to succeed.");
            profile.AddDiamonds(5L);
            Assert(profile.TrySpendDiamonds(8L), "Expected the diamond spend to succeed.");
            Assert(!profile.TrySpendDollars(1000L), "An unaffordable spend must not succeed.");
            profile.AddExp(250);

            Assert(profile.Dollars == 110L, "Dollar balance was incorrect.");
            Assert(profile.Diamonds == 7L, "Diamond balance was incorrect.");
            Assert(profile.Level == 3 && profile.Exp == 50, "EXP did not carry over through automatic level-ups.");
        }

        private static void CapsEarlyGameIncidentPenalties()
        {
            var profile = new PlayerProfile("early-player", level: 1, labUpgradeLevel: 1);
            var dirtyTools = new[]
            {
                new ToolState("one", true, ToolCleanState.Dirty, ToolStorageState.OnBench),
                new ToolState("two", true, ToolCleanState.NeedsWashing, ToolStorageState.OnBench)
            };
            var result = new IncidentService().EvaluateUncleanedExit(dirtyTools, profile);

            Assert(result.Severity == IncidentSeverity.CriticalExplosion, "The fixture must exercise the maximum base penalty.");
            Assert(result.PenaltyDollars == 35L, "Early-game incident penalty exceeded its safety cap.");
        }

        private static void DetectsDeadEndEconomyContent()
        {
            var validator = new DeadEndEconomyValidator();
            List<string> errors;
            Assert(validator.ValidateEconomy(CreateCatalogue(1, 1), out errors), "The baseline catalogue should be completable.");
            Assert(errors.Count == 0, "The baseline catalogue unexpectedly reported errors.");

            var deadEndCatalogue = CreateCatalogue(1, 1, inputUnlockLevel: 2);
            Assert(!validator.ValidateEconomy(deadEndCatalogue, out errors), "A level-one recipe requiring a level-two-only item must fail.");
            Assert(errors.Count > 0 && errors[0].IndexOf("Dead-end chemical 'water'", StringComparison.Ordinal) >= 0, "Dead-end diagnostic did not name the blocked chemical.");
        }

        private static ContentCatalogue CreateCatalogue(int reactionUnlockLevel, int toolUnlockLevel, int inputUnlockLevel = 1)
        {
            var water = CreateChemical("water", inputUnlockLevel, 10L);
            var salt = CreateChemical("salt", 1, 20L);
            var tool = CreateTool("beaker", toolUnlockLevel);
            var reaction = new Reaction("salt", "water -> salt", "condition", tool.Id, reactionUnlockLevel, salt.Id, 2m, 1m, "source", "reviewer", DateTimeOffset.UtcNow, "test");
            return new ContentCatalogue(
                new[] { water, salt },
                new[] { reaction },
                new[]
                {
                    new ReactionParticipant(reaction.Id, water.Id, ReactionParticipantRole.Reactant, 1, 2m, 1),
                    new ReactionParticipant(reaction.Id, salt.Id, ReactionParticipantRole.Product, 1, 2m, 2)
                },
                new ReactionObservation[0],
                new[] { tool },
                new QuestTemplate[0]);
        }

        private static ChemicalItem CreateChemical(string id, int unlockLevel, long price)
        {
            return new ChemicalItem(id, id + ".name", id, ElementGroup.Nonmetal, ChemicalState.Liquid, "clear", id + ".odor", SolubilityLevel.Soluble, HazardTier.Low, ItemRarity.Common, price, id + ".icon", "source", "reviewer", DateTimeOffset.UtcNow, "test", unlockLevel);
        }

        private static LabTool CreateTool(string id, int unlockLevel)
        {
            return new LabTool(id, id + ".name", LabToolType.Container, 100m, true, ToolCleanState.Clean, unlockLevel, 0L, id + ".visual", new[] { LabToolCapability.Contain, LabToolCapability.Pour }, "source", "reviewer", DateTimeOffset.UtcNow, "test");
        }

        private static Dictionary<string, decimal> Bench(string itemId, decimal grams)
        {
            return new Dictionary<string, decimal>(StringComparer.Ordinal) { { itemId, grams } };
        }

        private static void AssertStatus(ReactionResolveStatus expected, ReactionResolveResult actual)
        {
            Assert(actual != null && actual.Status == expected, "Expected reaction status " + expected + ", got " + (actual == null ? "null" : actual.Status.ToString()) + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
