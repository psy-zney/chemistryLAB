using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Application
{
    public enum ProductCollectionStatus
    {
        Collected = 0,
        AlreadyCollected = 1,
        InvalidReactionResult = 2,
        InvalidContainer = 3,
        InventoryCapacityExceeded = 4
    }

    /// <summary>Outcome of moving a resolved reaction output into the player inventory.</summary>
    public sealed class ProductCollectionResult
    {
        internal ProductCollectionResult(ProductCollectionStatus status, string message, string localisationKey, ReactionTransaction transaction)
        {
            Status = status;
            Message = message;
            LocalisationKey = localisationKey;
            Transaction = transaction;
        }

        public ProductCollectionStatus Status { get; private set; }
        public string Message { get; private set; }
        public string LocalisationKey { get; private set; }
        public ReactionTransaction Transaction { get; private set; }
    }

    /// <summary>
    /// Applies the final, player-visible part of a reaction. Persistence code should
    /// persist the returned transaction and the changed state in one save operation.
    /// </summary>
    public sealed class ProductRecoveryService
    {
        private readonly object syncRoot = new object();
        private readonly HashSet<string> collectedTransactionIds = new HashSet<string>(StringComparer.Ordinal);

        public ProductCollectionResult CollectProduct(
            ReactionResolveResult resolveResult,
            Inventory inventory,
            ToolState containerTool)
        {
            if (resolveResult == null || inventory == null)
            {
                return Failure(ProductCollectionStatus.InvalidReactionResult, "A successful reaction result and inventory are required.", "product.collect.invalid_result");
            }

            if (!IsCollectable(resolveResult))
            {
                return Failure(ProductCollectionStatus.InvalidReactionResult, "The reaction result cannot be collected.", "product.collect.invalid_result");
            }

            // ToolState deliberately contains player state only. In the absence of a
            // catalogue entry, ownership and cleanliness are the verifiable container rules.
            if (containerTool == null || !containerTool.IsOwned || containerTool.Cleanliness != ToolCleanState.Clean)
            {
                return Failure(ProductCollectionStatus.InvalidContainer, "Use an owned, clean container to collect the product.", "product.collect.invalid_container");
            }

            lock (syncRoot)
            {
                var sourceTransactionId = resolveResult.Transaction.TransactionId;
                if (collectedTransactionIds.Contains(sourceTransactionId))
                {
                    return Failure(ProductCollectionStatus.AlreadyCollected, "This reaction output was already collected.", "product.collect.already_collected");
                }

                var currentGram = inventory.GetGram(resolveResult.OutputId);
                if (currentGram > decimal.MaxValue - resolveResult.OutputMassGram)
                {
                    return Failure(ProductCollectionStatus.InventoryCapacityExceeded, "The inventory cannot hold this product quantity.", "product.collect.capacity_exceeded");
                }

                var transactionId = Guid.NewGuid().ToString("N");
                var outputs = new Dictionary<string, decimal>(StringComparer.Ordinal)
                {
                    { resolveResult.OutputId, resolveResult.OutputMassGram }
                };
                var transaction = new ReactionTransaction(
                    transactionId,
                    ReactionTransactionKind.CollectProduct,
                    new Dictionary<string, decimal>(StringComparer.Ordinal),
                    outputs,
                    0L,
                    0L,
                    "collect:" + sourceTransactionId,
                    DateTimeOffset.UtcNow);

                // All possible failures were checked above. Keep these mutations together
                // so callers observe either a completed collection or no collection.
                inventory.AddGram(resolveResult.OutputId, resolveResult.OutputMassGram);
                containerTool.SetCleanliness(ToolCleanState.Dirty);
                collectedTransactionIds.Add(sourceTransactionId);

                return new ProductCollectionResult(
                    ProductCollectionStatus.Collected,
                    "Product collected successfully.",
                    "product.collect.success",
                    transaction);
            }
        }

        private static bool IsCollectable(ReactionResolveResult result)
        {
            if (result.Status != ReactionResolveStatus.Success
                || result.Transaction == null
                || result.Transaction.Kind != ReactionTransactionKind.Craft
                || string.IsNullOrWhiteSpace(result.OutputId)
                || result.OutputMassGram <= 0m)
            {
                return false;
            }

            decimal transactionOutput;
            return result.Transaction.Outputs.TryGetValue(result.OutputId, out transactionOutput)
                && transactionOutput == result.OutputMassGram;
        }

        private static ProductCollectionResult Failure(ProductCollectionStatus status, string message, string localisationKey)
        {
            return new ProductCollectionResult(status, message, localisationKey, null);
        }
    }
}
