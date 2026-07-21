using System;
using System.Collections.Generic;
using ChemistryLab.Application;
using ChemistryLab.Domain;
using ChemistryLab.Infrastructure;

namespace ChemistryLab.Presentation
{
    public enum ShopPurchaseStatus
    {
        Purchased = 0,
        InvalidItem = 1,
        InvalidQuantity = 2,
        Locked = 3,
        InsufficientDollars = 4,
        AlreadyOwned = 5,
        Processing = 6,
        TransactionRejected = 7,
        Failed = 8
    }

    public sealed class ShopPurchaseResult
    {
        internal ShopPurchaseResult(ShopPurchaseStatus status, string itemId, string message)
        {
            Status = status;
            ItemId = itemId;
            Message = message;
        }

        public ShopPurchaseStatus Status { get; private set; }
        public string ItemId { get; private set; }
        public string Message { get; private set; }
        public bool IsSuccess { get { return Status == ShopPurchaseStatus.Purchased; } }
    }

    /// <summary>Purchases catalogue chemicals and tools through durable save transactions.</summary>
    public sealed class ShopPresenter
    {
        private readonly object syncRoot = new object();
        private readonly ContentCatalogue catalogue;
        private readonly ISaveRepository saveRepository;
        private readonly HashSet<string> processingPurchases = new HashSet<string>(StringComparer.Ordinal);

        public ShopPresenter(ContentCatalogue catalogue, ISaveRepository saveRepository)
        {
            this.catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            this.saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
        }

        public bool CanBuyItem(string itemId, int quantity)
        {
            var item = FindChemical(itemId);
            var profile = saveRepository.LoadProfile();
            long total;
            return item != null && quantity > 0 && profile != null && profile.Level >= item.UnlockLevel && TryGetTotalPrice(item.Price, quantity, out total) && profile.Dollars >= total;
        }

        public ShopPurchaseResult BuyItem(string itemId, int quantity)
        {
            var item = FindChemical(itemId);
            if (item == null) return Failure(ShopPurchaseStatus.InvalidItem, itemId, "The chemical does not exist.");
            if (quantity <= 0) return Failure(ShopPurchaseStatus.InvalidQuantity, itemId, "Quantity must be greater than zero.");
            long total;
            if (!TryGetTotalPrice(item.Price, quantity, out total)) return Failure(ShopPurchaseStatus.Failed, itemId, "The total price is too large.");
            if (!TryStart("chemical:" + itemId)) return Failure(ShopPurchaseStatus.Processing, itemId, "This purchase is already being processed.");

            try
            {
                var profile = saveRepository.LoadProfile();
                if (profile == null || profile.Level < item.UnlockLevel) return Failure(ShopPurchaseStatus.Locked, itemId, "The required player level has not been reached.");
                if (profile.Dollars < total) return Failure(ShopPurchaseStatus.InsufficientDollars, itemId, "Not enough dollars.");
                var outputs = new Dictionary<string, decimal>(StringComparer.Ordinal) { { item.Id, quantity } };
                var transaction = CreateTransaction(outputs, -total, "shop:chemical:" + item.Id);
                var committed = saveRepository.CommitTransaction(transaction, delegate
                {
                    if (total > 0L && !saveRepository.Profile.TrySpendDollars(total)) throw new InvalidOperationException("Dollars changed during purchase.");
                    saveRepository.Inventory.AddGram(item.Id, quantity);
                });
                return committed ? Success(itemId) : Failure(ShopPurchaseStatus.TransactionRejected, itemId, "The purchase was already processed.");
            }
            catch (Exception) { return Failure(ShopPurchaseStatus.Failed, itemId, "The purchase could not be completed."); }
            finally { Finish("chemical:" + itemId); }
        }

        public ShopPurchaseResult BuyTool(string toolId)
        {
            var tool = FindTool(toolId);
            if (tool == null) return Failure(ShopPurchaseStatus.InvalidItem, toolId, "The tool does not exist.");
            if (!TryStart("tool:" + toolId)) return Failure(ShopPurchaseStatus.Processing, toolId, "This purchase is already being processed.");

            try
            {
                var profile = saveRepository.LoadProfile();
                if (profile == null || profile.Level < tool.UnlockLevel) return Failure(ShopPurchaseStatus.Locked, toolId, "The required player level has not been reached.");
                if (IsToolOwned(tool)) return Failure(ShopPurchaseStatus.AlreadyOwned, toolId, "This tool is already owned.");
                if (profile.Dollars < tool.Price) return Failure(ShopPurchaseStatus.InsufficientDollars, toolId, "Not enough dollars.");
                var transaction = CreateTransaction(new Dictionary<string, decimal>(StringComparer.Ordinal), -tool.Price, "shop:tool:" + tool.Id);
                var committed = saveRepository.CommitTransaction(transaction, delegate
                {
                    if (tool.Price > 0L && !saveRepository.Profile.TrySpendDollars(tool.Price)) throw new InvalidOperationException("Dollars changed during purchase.");
                    var state = FindRepositoryToolState(tool.Id);
                    if (state == null) saveRepository.Tools.Add(new ToolState(tool.Id, true, tool.CleanState, ToolStorageState.Stored));
                    else state.SetOwned(true);
                });
                return committed ? Success(toolId) : Failure(ShopPurchaseStatus.TransactionRejected, toolId, "The purchase was already processed.");
            }
            catch (Exception) { return Failure(ShopPurchaseStatus.Failed, toolId, "The purchase could not be completed."); }
            finally { Finish("tool:" + toolId); }
        }

        private bool TryStart(string key) { lock (syncRoot) return processingPurchases.Add(key); }
        private void Finish(string key) { lock (syncRoot) { processingPurchases.Remove(key); } }
        private bool IsToolOwned(LabTool tool)
        {
            var state = FindToolState(tool.Id);
            return tool.IsOwned || (state != null && state.IsOwned);
        }
        private ToolState FindToolState(string toolId)
        {
            var tools = saveRepository.LoadTools();
            for (var index = 0; index < tools.Count; index++) if (tools[index] != null && string.Equals(tools[index].ToolId, toolId, StringComparison.Ordinal)) return tools[index];
            return null;
        }
        private ToolState FindRepositoryToolState(string toolId)
        {
            for (var index = 0; index < saveRepository.Tools.Count; index++) if (saveRepository.Tools[index] != null && string.Equals(saveRepository.Tools[index].ToolId, toolId, StringComparison.Ordinal)) return saveRepository.Tools[index];
            return null;
        }
        private ChemicalItem FindChemical(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return null;
            for (var index = 0; index < catalogue.ChemicalItems.Count; index++) if (catalogue.ChemicalItems[index] != null && string.Equals(catalogue.ChemicalItems[index].Id, itemId, StringComparison.Ordinal)) return catalogue.ChemicalItems[index];
            return null;
        }
        private LabTool FindTool(string toolId)
        {
            if (string.IsNullOrWhiteSpace(toolId)) return null;
            for (var index = 0; index < catalogue.LabTools.Count; index++) if (catalogue.LabTools[index] != null && string.Equals(catalogue.LabTools[index].Id, toolId, StringComparison.Ordinal)) return catalogue.LabTools[index];
            return null;
        }
        private static bool TryGetTotalPrice(long price, int quantity, out long total)
        {
            try { total = checked(price * (long)quantity); return true; }
            catch (OverflowException) { total = 0L; return false; }
        }
        private static ReactionTransaction CreateTransaction(IReadOnlyDictionary<string, decimal> outputs, long dollarDelta, string idempotencyPrefix)
        {
            var id = Guid.NewGuid().ToString("N");
            return new ReactionTransaction(id, ReactionTransactionKind.Buy, new Dictionary<string, decimal>(StringComparer.Ordinal), outputs, dollarDelta, 0L, idempotencyPrefix + ":" + id, DateTimeOffset.UtcNow);
        }
        private static ShopPurchaseResult Success(string itemId) { return new ShopPurchaseResult(ShopPurchaseStatus.Purchased, itemId, "Purchase completed."); }
        private static ShopPurchaseResult Failure(ShopPurchaseStatus status, string itemId, string message) { return new ShopPurchaseResult(status, itemId, message); }
    }
}
