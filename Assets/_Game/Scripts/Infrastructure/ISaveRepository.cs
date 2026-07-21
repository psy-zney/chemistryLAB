using System;
using System.Collections.Generic;
using ChemistryLab.Application;
using ChemistryLab.Domain;

namespace ChemistryLab.Infrastructure
{
    /// <summary>Health reported when opening a persisted player save.</summary>
    public enum SaveHealthStatus
    {
        Healthy = 0,
        CorruptedNeedsRecovery = 1,
        NoData = 2
    }

    /// <summary>
    /// Portable snapshot used for rollback, backup, and migration. Its contents are
    /// cloned by the repository, so callers cannot mutate a stored snapshot.
    /// </summary>
    public sealed class SaveSnapshot
    {
        public SaveSnapshot(
            PlayerProfile profile,
            IDictionary<string, decimal> inventory,
            IEnumerable<ToolState> tools,
            IEnumerable<ReactionTransaction> transactions)
        {
            Profile = profile;
            Inventory = inventory;
            Tools = tools;
            Transactions = transactions;
        }

        public PlayerProfile Profile { get; private set; }
        public IDictionary<string, decimal> Inventory { get; private set; }
        public IEnumerable<ToolState> Tools { get; private set; }
        public IEnumerable<ReactionTransaction> Transactions { get; private set; }
    }

    /// <summary>Persistence boundary for profile, inventory, tools, and transaction logs.</summary>
    public interface ISaveRepository
    {
        bool HasData { get; }
        bool IsCorrupted { get; }

        // These are the mutable working state used inside CommitTransaction only.
        PlayerProfile Profile { get; }
        Inventory Inventory { get; }
        IList<ToolState> Tools { get; }

        PlayerProfile LoadProfile();
        IReadOnlyDictionary<string, decimal> LoadInventory();
        IReadOnlyList<ToolState> LoadTools();
        IReadOnlyList<ReactionTransaction> LoadTransactions();

        void SaveState(PlayerProfile profile, IDictionary<string, decimal> inventory, IEnumerable<ToolState> tools);
        void SaveProfile(PlayerProfile profile);
        void SaveInventory(IDictionary<string, decimal> inventory);
        void SaveTools(IEnumerable<ToolState> tools);

        /// <summary>
        /// Runs the supplied mutations and appends the transaction log in one durable
        /// operation. A duplicate idempotency key is ignored and returns false.
        /// </summary>
        bool CommitTransaction(ReactionTransaction transaction, Action applyMutations);

        SaveSnapshot CaptureSnapshot();
        void RestoreSnapshot(SaveSnapshot snapshot);

        void CreateBackup(string backupId);
        bool RestoreBackup(string backupId);
        bool HasBackup(string backupId);
        void CopyBackup(string sourceBackupId, string destinationBackupId);
        void DeleteBackup(string backupId);
    }
}
