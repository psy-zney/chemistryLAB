using System;
using System.Collections.Generic;
using System.IO;
using ChemistryLab.Application;
using ChemistryLab.Domain;

namespace ChemistryLab.Infrastructure
{
    /// <summary>
    /// Framework-free persistence regression suite. Compile with
    /// PERSISTENCE_STANDALONE to expose a console entry point, or invoke RunAll from
    /// a Unity/editor test wrapper.
    /// </summary>
    public static class PersistenceIntegrationTests
    {
        public static void RunAll()
        {
            var root = Path.Combine(Path.GetTempPath(), "ChemistryLab.Persistence." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                SavesAndReloadsAnAtomicTransaction(Path.Combine(root, "player.save"));
                RecoversFromCorruptedSave(Path.Combine(root, "recovery.save"));
                RestoresBackupWhenMigrationFails(Path.Combine(root, "migration.save"));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static void SavesAndReloadsAnAtomicTransaction(string savePath)
        {
            var repository = new SaveRepository(savePath);
            var profile = new PlayerProfile("integration-player");
            var inventory = new Dictionary<string, decimal>(StringComparer.Ordinal);
            var cleanTool = new ToolState("beaker", true, ToolCleanState.Clean, ToolStorageState.Stored);
            repository.SaveState(profile, inventory, new ToolState[0]);

            var transaction = new ReactionTransaction(
                "transaction-1", ReactionTransactionKind.Craft,
                new Dictionary<string, decimal>(StringComparer.Ordinal),
                new Dictionary<string, decimal>(StringComparer.Ordinal) { { "NaCl", 50.5m } },
                1000L, 0L, "integration:transaction-1", DateTimeOffset.UtcNow);
            Assert(repository.CommitTransaction(transaction, delegate
            {
                repository.Profile.AddDollars(1000L);
                repository.Inventory.AddGram("NaCl", 50.5m);
                repository.Tools.Add(cleanTool);
            }), "The initial transaction must commit.");

            // A new repository instance is the standalone equivalent of closing and reopening the app.
            var reopened = new SaveRepository(savePath);
            Assert(reopened.LoadProfile().Dollars == 1000L, "Dollars were not preserved.");
            Assert(reopened.LoadInventory()["NaCl"] == 50.5m, "NaCl grams were not preserved.");
            Assert(reopened.LoadTools()[0].Cleanliness == ToolCleanState.Clean, "Tool cleanliness was not preserved.");
            Assert(reopened.LoadTransactions().Count == 1, "Transaction log was not persisted.");
            Assert(!reopened.CommitTransaction(transaction, delegate { throw new InvalidOperationException("Duplicate should not execute."); }), "Idempotency did not reject the duplicate transaction.");
        }

        private static void RecoversFromCorruptedSave(string savePath)
        {
            var repository = new SaveRepository(savePath);
            repository.SaveState(
                new PlayerProfile("recovery-player", dollars: 1000L),
                new Dictionary<string, decimal>(StringComparer.Ordinal) { { "NaCl", 50.5m } },
                new[] { new ToolState("beaker", true, ToolCleanState.Clean, ToolStorageState.Stored) });
            var recovery = new BackupRecoveryService();
            recovery.CreateBackup(repository);

            File.WriteAllText(savePath, "Corrupted");
            var corruptedRepository = new SaveRepository(savePath);
            Assert(recovery.InspectSaveHealth(corruptedRepository) == SaveHealthStatus.CorruptedNeedsRecovery, "Corruption was not detected.");
            Assert(recovery.RestoreFromBackup(corruptedRepository), "Backup restore did not run.");
            Assert(recovery.InspectSaveHealth(corruptedRepository) == SaveHealthStatus.Healthy, "Save was not healthy after restore.");
            Assert(corruptedRepository.LoadProfile().Dollars == 1000L, "Backup did not restore dollars.");
            Assert(corruptedRepository.LoadInventory()["NaCl"] == 50.5m, "Backup did not restore inventory.");
        }

        private static void RestoresBackupWhenMigrationFails(string savePath)
        {
            var repository = new SaveRepository(savePath);
            repository.SaveState(
                new PlayerProfile("migration-player", dollars: 1000L, schemaVersion: 1),
                new Dictionary<string, decimal>(StringComparer.Ordinal) { { "NaCl", 50.5m } },
                new ToolState[0]);

            var migration = new MigrationService();
            migration.RegisterMigration(1, delegate(PlayerProfile profile, ISaveRepository ignored)
            {
                profile.AddDollars(1L);
                // Deliberately omit SetSchemaVersion: the service must reject this
                // mismatch and restore the backup made immediately before migration.
            });

            var failed = false;
            try { migration.Migrate(repository, 2); }
            catch (InvalidOperationException) { failed = true; }
            Assert(failed, "The invalid migration must fail.");
            Assert(repository.LoadProfile().SchemaVersion == 1, "Failed migration changed the schema version.");
            Assert(repository.LoadProfile().Dollars == 1000L, "Failed migration did not restore the original profile.");
            Assert(repository.LoadInventory()["NaCl"] == 50.5m, "Failed migration did not restore inventory.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

#if PERSISTENCE_STANDALONE
        public static void Main()
        {
            RunAll();
            Console.WriteLine("Persistence integration tests passed.");
        }
#endif
    }
}
