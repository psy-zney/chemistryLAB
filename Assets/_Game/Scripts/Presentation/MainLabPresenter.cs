using System;
using System.Collections.Generic;
using ChemistryLab.Application;
using ChemistryLab.Domain;
using ChemistryLab.Infrastructure;

namespace ChemistryLab.Presentation
{
    /// <summary>One-way output port for the Main Lab scene. Implement this in a Unity view adapter.</summary>
    public interface IMainLabView
    {
        void ShowBench(IReadOnlyDictionary<string, decimal> selectedItems, string selectedToolId);
        void ShowLocalisedMessage(string localisationKey);
        void ShowExperimentReady(string reactionId, string outputItemId, decimal outputMassGram);
        void ShowProductCollected(string itemId, decimal massGram);
        void RefreshTool(string toolId, ToolCleanState cleanliness);
    }

    /// <summary>
    /// Coordinates user intent for the lab. This class contains no Unity UI calls:
    /// input enters through On* methods and state is emitted only through IMainLabView.
    /// </summary>
    public sealed class MainLabPresenter
    {
        private readonly ContentCatalogue catalogue;
        private readonly ISaveRepository saveRepository;
        private readonly IMainLabView view;
        private readonly ReactionResolver reactionResolver;
        private readonly WashingService washingService;
        private readonly Dictionary<string, decimal> benchItems = new Dictionary<string, decimal>(StringComparer.Ordinal);

        private string selectedToolId;
        private string selectedReactionId;
        private ReactionResolveResult pendingResult;

        public MainLabPresenter(
            ContentCatalogue catalogue,
            ISaveRepository saveRepository,
            IMainLabView view,
            ReactionResolver reactionResolver = null,
            WashingService washingService = null)
        {
            this.catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            this.saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.reactionResolver = reactionResolver ?? new ReactionResolver();
            this.washingService = washingService ?? new WashingService();
        }

        public string SelectedReactionId { get { return selectedReactionId; } }
        public string SelectedToolId { get { return selectedToolId; } }

        /// <summary>Called by a recipe card; no reaction is inferred from chemical names at runtime.</summary>
        public void OnSelectReaction(string reactionId)
        {
            if (FindReaction(reactionId) == null)
            {
                view.ShowLocalisedMessage("lab.reaction.invalid_selection");
                return;
            }

            selectedReactionId = reactionId;
            PublishBench();
        }

        public void OnSelectChemical(string itemId, decimal massGram)
        {
            if (pendingResult != null)
            {
                view.ShowLocalisedMessage("lab.experiment.collect_product_first");
                return;
            }

            if (FindChemical(itemId) == null || massGram <= 0m)
            {
                view.ShowLocalisedMessage("lab.chemical.invalid_selection");
                return;
            }

            var inventory = saveRepository.LoadInventory();
            decimal available;
            if (!inventory.TryGetValue(itemId, out available) || available < massGram)
            {
                view.ShowLocalisedMessage("lab.chemical.insufficient_inventory");
                return;
            }

            benchItems[itemId] = massGram;
            PublishBench();
        }

        public void OnSelectTool(string toolId)
        {
            if (pendingResult != null)
            {
                view.ShowLocalisedMessage("lab.experiment.collect_product_first");
                return;
            }

            var tool = FindTool(toolId);
            var state = FindToolState(toolId);
            if (tool == null || state == null || !state.IsOwned)
            {
                view.ShowLocalisedMessage("lab.tool.invalid_selection");
                return;
            }

            selectedToolId = toolId;
            PublishBench();
        }

        public void OnExecuteExperiment()
        {
            if (pendingResult != null)
            {
                view.ShowLocalisedMessage("lab.experiment.collect_product_first");
                return;
            }

            var reaction = FindReaction(selectedReactionId);
            var tool = CreateRuntimeTool(FindTool(selectedToolId), FindToolState(selectedToolId));
            var result = reactionResolver.Resolve(reaction, benchItems, tool, saveRepository.LoadProfile(), catalogue);
            if (result.Status != ReactionResolveStatus.Success)
            {
                view.ShowLocalisedMessage(result.LocalisationKey);
                return;
            }

            try
            {
                var committed = saveRepository.CommitTransaction(result.Transaction, delegate
                {
                    foreach (var input in result.Transaction.Inputs)
                    {
                        if (!saveRepository.Inventory.RemoveGram(input.Key, input.Value))
                        {
                            throw new InvalidOperationException("Inventory changed while the reaction was being committed.");
                        }
                    }
                });

                if (!committed)
                {
                    view.ShowLocalisedMessage("lab.experiment.already_processed");
                    return;
                }
            }
            catch (Exception)
            {
                view.ShowLocalisedMessage("lab.experiment.transaction_failed");
                return;
            }

            benchItems.Clear();
            pendingResult = result;
            view.ShowExperimentReady(result.ReactionId, result.OutputId, result.OutputMassGram);
            PublishBench();
        }

        public void OnCollectProduct()
        {
            if (pendingResult == null)
            {
                view.ShowLocalisedMessage("product.collect.invalid_result");
                return;
            }

            var container = FindToolState(selectedToolId);
            if (container == null)
            {
                view.ShowLocalisedMessage("product.collect.invalid_container");
                return;
            }

            // Validate against clones first. ProductRecoveryService mutates its inputs,
            // therefore real repository state is changed only inside CommitTransaction.
            var validationInventory = new Inventory(saveRepository.LoadInventory());
            var validationTool = CloneToolState(container);
            var collection = new ProductRecoveryService().CollectProduct(pendingResult, validationInventory, validationTool);
            if (collection.Status != ProductCollectionStatus.Collected)
            {
                view.ShowLocalisedMessage(collection.LocalisationKey);
                return;
            }

            try
            {
                var committed = saveRepository.CommitTransaction(collection.Transaction, delegate
                {
                    saveRepository.Inventory.AddGram(pendingResult.OutputId, pendingResult.OutputMassGram);
                    var repositoryTool = FindRepositoryToolState(selectedToolId);
                    if (repositoryTool == null) throw new InvalidOperationException("The selected tool no longer exists.");
                    repositoryTool.SetCleanliness(ToolCleanState.Dirty);
                });

                if (!committed)
                {
                    view.ShowLocalisedMessage("product.collect.already_collected");
                    return;
                }
            }
            catch (Exception)
            {
                view.ShowLocalisedMessage("product.collect.transaction_failed");
                return;
            }

            view.ShowProductCollected(pendingResult.OutputId, pendingResult.OutputMassGram);
            view.RefreshTool(selectedToolId, ToolCleanState.Dirty);
            pendingResult = null;
        }

        public void OnWashTool(string toolId)
        {
            var current = FindToolState(toolId);
            if (current == null)
            {
                view.ShowLocalisedMessage("tool.wash.invalid_tool");
                return;
            }

            var proposed = CloneToolState(current);
            var result = washingService.WashTool(proposed, saveRepository.LoadProfile());
            if (result.Status == WashingStatus.InvalidProfile || result.Status == WashingStatus.InvalidTool)
            {
                view.ShowLocalisedMessage(result.LocalisationKey);
                return;
            }

            try
            {
                var allTools = new List<ToolState>(saveRepository.LoadTools());
                ReplaceToolState(allTools, proposed);
                saveRepository.SaveTools(allTools);
            }
            catch (Exception)
            {
                view.ShowLocalisedMessage("tool.wash.transaction_failed");
                return;
            }

            view.ShowLocalisedMessage(result.LocalisationKey);
            view.RefreshTool(toolId, proposed.Cleanliness);
        }

        private void PublishBench()
        {
            view.ShowBench(new Dictionary<string, decimal>(benchItems, StringComparer.Ordinal), selectedToolId);
        }

        private ChemicalItem FindChemical(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (var index = 0; index < catalogue.ChemicalItems.Count; index++)
                if (catalogue.ChemicalItems[index] != null && string.Equals(catalogue.ChemicalItems[index].Id, id, StringComparison.Ordinal)) return catalogue.ChemicalItems[index];
            return null;
        }

        private Reaction FindReaction(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (var index = 0; index < catalogue.Reactions.Count; index++)
                if (catalogue.Reactions[index] != null && string.Equals(catalogue.Reactions[index].Id, id, StringComparison.Ordinal)) return catalogue.Reactions[index];
            return null;
        }

        private LabTool FindTool(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (var index = 0; index < catalogue.LabTools.Count; index++)
                if (catalogue.LabTools[index] != null && string.Equals(catalogue.LabTools[index].Id, id, StringComparison.Ordinal)) return catalogue.LabTools[index];
            return null;
        }

        private ToolState FindToolState(string id)
        {
            var tools = saveRepository.LoadTools();
            for (var index = 0; index < tools.Count; index++)
                if (tools[index] != null && string.Equals(tools[index].ToolId, id, StringComparison.Ordinal)) return tools[index];
            return null;
        }

        private ToolState FindRepositoryToolState(string id)
        {
            var tools = saveRepository.Tools;
            for (var index = 0; index < tools.Count; index++)
                if (tools[index] != null && string.Equals(tools[index].ToolId, id, StringComparison.Ordinal)) return tools[index];
            return null;
        }

        private static LabTool CreateRuntimeTool(LabTool catalogueTool, ToolState playerState)
        {
            if (catalogueTool == null || playerState == null) return null;
            return new LabTool(catalogueTool.Id, catalogueTool.NameKey, catalogueTool.Type, catalogueTool.CapacityGram, playerState.IsOwned,
                playerState.Cleanliness, catalogueTool.UnlockLevel, catalogueTool.Price, catalogueTool.VisualAddressKey, catalogueTool.Capabilities,
                catalogueTool.SourceRef, catalogueTool.Reviewer, catalogueTool.ReviewedAt, catalogueTool.ContentVersion);
        }

        private static ToolState CloneToolState(ToolState source)
        {
            return new ToolState(source.ToolId, source.IsOwned, source.Cleanliness, source.StorageState);
        }

        private static void ReplaceToolState(List<ToolState> tools, ToolState replacement)
        {
            for (var index = 0; index < tools.Count; index++)
            {
                if (string.Equals(tools[index].ToolId, replacement.ToolId, StringComparison.Ordinal))
                {
                    tools[index] = replacement;
                    return;
                }
            }

            throw new InvalidOperationException("The tool to replace does not exist.");
        }
    }
}
