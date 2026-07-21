using System;
using System.Collections.Generic;
using System.IO;
using ChemistryLab.Domain;
using ChemistryLab.Infrastructure;
using ChemistryLab.Presentation;

namespace ChemistryLab.Application
{
    /// <summary>
    /// Framework-free smoke tests for the playable quest, crafting, recovery, and
    /// persistence loop. It only writes uniquely named files in the OS temp folder.
    /// </summary>
    public static class GameplayIntegrationTests
    {
        public static void RunAll()
        {
            var root = Path.Combine(Path.GetTempPath(), "ChemistryLab.Gameplay." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                SuccessPath(Path.Combine(root, "success.save"));
                FailureAndRecovery(Path.Combine(root, "failure.save"));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        private static void SuccessPath(string savePath)
        {
            var catalogue = CreateCatalogue();
            var repository = new SaveRepository(savePath);
            var profile = new PlayerProfile("success-player", dollars: 500L);
            var beakerState = new ToolState("beaker", true, ToolCleanState.Clean, ToolStorageState.Stored);
            repository.SaveState(profile, new Dictionary<string, decimal>(StringComparer.Ordinal), new[] { beakerState });

            var questTemplate = catalogue.QuestTemplates[0];
            var quest = new QuestService().GenerateQuestInstance(questTemplate, 17);
            var shopResult = new ShopPresenter(catalogue, repository).BuyItem("water", 2);
            Assert(shopResult.Status == ShopPurchaseStatus.Purchased, "Could not buy the quest ingredient.");

            decimal splitGram;
            Assert(repository.Inventory.SplitGram("water", 2m, out splitGram) && splitGram == 2m, "Could not split the purchased grams onto the bench.");
            var reaction = catalogue.Reactions[0];
            var resolved = new ReactionResolver().Resolve(
                reaction,
                Bench("water", splitGram),
                catalogue.LabTools[0],
                repository.Profile,
                catalogue);
            Assert(resolved.Status == ReactionResolveStatus.Success, "The valid recipe did not resolve.");
            Assert(repository.CommitTransaction(resolved.Transaction, delegate { }), "Craft transaction was not persisted.");

            var recovery = new ProductRecoveryService();
            var collected = recovery.CollectProduct(resolved, repository.Inventory, repository.Tools[0]);
            Assert(collected.Status == ProductCollectionStatus.Collected, "Could not collect the resolved product.");
            Assert(repository.CommitTransaction(collected.Transaction, delegate { }), "Collection transaction was not persisted.");

            var washing = new WashingService().WashTool(repository.Tools[0], repository.Profile);
            Assert(washing.Status == WashingStatus.Washed, "The dirty collection container was not washed.");
            repository.SaveTools(repository.Tools);

            var delivered = new QuestService().ValidateAndDeliver(quest, questTemplate, repository.Inventory, repository.Profile);
            Assert(delivered.Status == QuestDeliveryStatus.Delivered, "The collected product could not be delivered to the quest.");
            Assert(repository.CommitTransaction(delivered.Transaction, delegate { }), "Quest reward transaction was not persisted.");

            var reopened = new SaveRepository(savePath);
            var reloadedProfile = reopened.LoadProfile();
            var reloadedInventory = reopened.LoadInventory();
            Assert(reloadedProfile.Dollars == 520L, "Dollars changed across the full gameplay loop or reload.");
            Assert(reloadedProfile.Level == 2 && reloadedProfile.Exp == 25, "Quest EXP or automatic level-up was not retained on reload.");
            Assert(reloadedInventory["salt"] == 1m, "The undelivered portion of the product was not retained.");
            Assert(!reloadedInventory.ContainsKey("water"), "Reactant grams remained after crafting.");
            Assert(reopened.LoadTools()[0].Cleanliness == ToolCleanState.Clean, "Washed tool cleanliness was not retained.");
            Assert(reopened.LoadTransactions().Count == 4, "Expected buy, craft, collection, and quest transactions.");
        }

        private static void FailureAndRecovery(string savePath)
        {
            var catalogue = CreateCatalogue();
            var repository = new SaveRepository(savePath);
            repository.SaveState(
                new PlayerProfile("failure-player", dollars: 100L),
                new Dictionary<string, decimal>(StringComparer.Ordinal) { { "water", 2m } },
                new[] { new ToolState("beaker", true, ToolCleanState.Dirty, ToolStorageState.OnBench) });

            var before = repository.LoadInventory()["water"];
            var reaction = catalogue.Reactions[0];
            var resolver = new ReactionResolver();
            var missingInput = resolver.Resolve(reaction, new Dictionary<string, decimal>(StringComparer.Ordinal), catalogue.LabTools[0], repository.Profile, catalogue);
            Assert(missingInput.Status == ReactionResolveStatus.MissingInput, "Missing input did not return the expected reaction status.");
            Assert(repository.Inventory.GetGram("water") == before, "A rejected reaction changed the inventory.");

            var successfulResolution = resolver.Resolve(reaction, Bench("water", 2m), catalogue.LabTools[0], repository.Profile, catalogue);
            Assert(successfulResolution.Status == ReactionResolveStatus.Success, "Failure fixture could not obtain a valid result for recovery testing.");
            var collection = new ProductRecoveryService().CollectProduct(successfulResolution, repository.Inventory, repository.Tools[0]);
            Assert(collection.Status == ProductCollectionStatus.InvalidContainer, "A dirty container was accepted for product recovery.");
            Assert(repository.Inventory.GetGram("water") == before && repository.Inventory.GetGram("salt") == 0m, "Failed recovery changed the inventory.");

            var incident = new IncidentService().EvaluateUncleanedExit(repository.Tools, repository.Profile);
            Assert(incident.PenaltyDollars == 25L, "The dirty-container incident penalty was incorrect.");

            repository.SaveState(repository.Profile, Copy(repository.Inventory.Quantities), repository.Tools);
            var reopened = new SaveRepository(savePath);
            Assert(reopened.LoadInventory()["water"] == before, "Inventory integrity did not survive reload after failure handling.");
            Assert(!reopened.LoadInventory().ContainsKey("salt"), "A failed recovery produced inventory output after reload.");
        }

        private static ContentCatalogue CreateCatalogue()
        {
            var water = CreateChemical("water", 1, 20L);
            var salt = CreateChemical("salt", 1, 0L);
            var beaker = new LabTool("beaker", "beaker.name", LabToolType.Container, 100m, true, ToolCleanState.Clean, 1, 0L, "beaker.visual", new[] { LabToolCapability.Contain, LabToolCapability.Pour }, "source", "reviewer", DateTimeOffset.UtcNow, "test");
            var reaction = new Reaction("salt", "water -> salt", "condition", beaker.Id, 1, salt.Id, 2m, 1m, "source", "reviewer", DateTimeOffset.UtcNow, "test");
            var quest = new QuestTemplate("first-salt", QuestTier.Tutorial, reaction.Id, "quest.dialogue", "quest.application", new QuestRewardRule(60L, 125, new string[0]), new QuestPrerequisite[0], "source", "reviewer", DateTimeOffset.UtcNow, "test");
            return new ContentCatalogue(
                new[] { water, salt },
                new[] { reaction },
                new[]
                {
                    new ReactionParticipant(reaction.Id, water.Id, ReactionParticipantRole.Reactant, 1, 2m, 1),
                    new ReactionParticipant(reaction.Id, salt.Id, ReactionParticipantRole.Product, 1, 2m, 2)
                },
                new ReactionObservation[0],
                new[] { beaker },
                new[] { quest });
        }

        private static ChemicalItem CreateChemical(string id, int unlockLevel, long price)
        {
            return new ChemicalItem(id, id + ".name", id, ElementGroup.Nonmetal, ChemicalState.Liquid, "clear", id + ".odor", SolubilityLevel.Soluble, HazardTier.Low, ItemRarity.Common, price, id + ".icon", "source", "reviewer", DateTimeOffset.UtcNow, "test", unlockLevel);
        }

        private static Dictionary<string, decimal> Bench(string itemId, decimal grams)
        {
            return new Dictionary<string, decimal>(StringComparer.Ordinal) { { itemId, grams } };
        }

        private static Dictionary<string, decimal> Copy(IReadOnlyDictionary<string, decimal> values)
        {
            var copy = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                copy.Add(value.Key, value.Value);
            }

            return copy;
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
