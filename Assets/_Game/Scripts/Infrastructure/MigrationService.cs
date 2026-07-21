using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Infrastructure
{
    /// <summary>Runs ordered, schema-version migrations with rollback to a pre-upgrade backup.</summary>
    public sealed class MigrationService
    {
        private readonly BackupRecoveryService backupRecovery;
        private readonly Dictionary<int, Action<PlayerProfile, ISaveRepository>> migrations;

        public MigrationService(BackupRecoveryService backupRecovery = null)
        {
            this.backupRecovery = backupRecovery ?? new BackupRecoveryService();
            migrations = new Dictionary<int, Action<PlayerProfile, ISaveRepository>>();
        }

        /// <summary>Registers the migration from <paramref name="fromSchemaVersion"/> to the next version.</summary>
        public void RegisterMigration(int fromSchemaVersion, Action<PlayerProfile, ISaveRepository> migration)
        {
            if (fromSchemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(fromSchemaVersion));
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            migrations[fromSchemaVersion] = migration;
        }

        public void Migrate(ISaveRepository repository, int targetSchemaVersion)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (targetSchemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(targetSchemaVersion));
            if (backupRecovery.InspectSaveHealth(repository) == SaveHealthStatus.CorruptedNeedsRecovery)
                throw new InvalidOperationException("The save is corrupted and must be recovered before migration.");
            if (!repository.HasData) return;

            var current = repository.Profile.SchemaVersion;
            if (current > targetSchemaVersion)
                throw new InvalidOperationException("Downgrading a save schema is not supported.");
            if (current == targetSchemaVersion) return;

            backupRecovery.CreateBackup(repository);
            try
            {
                while (repository.Profile.SchemaVersion < targetSchemaVersion)
                {
                    var fromVersion = repository.Profile.SchemaVersion;
                    Action<PlayerProfile, ISaveRepository> migration;
                    if (migrations.TryGetValue(fromVersion, out migration)) migration(repository.Profile, repository);
                    else repository.Profile.SetSchemaVersion(fromVersion + 1);

                    if (repository.Profile.SchemaVersion != fromVersion + 1)
                        throw new InvalidOperationException("Migration did not advance exactly one schema version.");

                    // Persist after every verified step, so a process stop has a valid
                    // on-disk state and the original backup remains available.
                    repository.SaveProfile(repository.Profile);
                }
            }
            catch
            {
                // Prefer the durable backup over an in-memory rollback: this also
                // recovers a failure occurring while a migration save is being written.
                if (!backupRecovery.RestoreFromBackup(repository))
                    throw new InvalidOperationException("Migration failed and no usable backup was available.");
                throw;
            }
        }
    }
}
