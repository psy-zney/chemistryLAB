using System;

namespace ChemistryLab.Infrastructure
{
    /// <summary>Creates a small rotating set of valid save backups and restores the newest one.</summary>
    public sealed class BackupRecoveryService
    {
        private const string BackupPrefix = "save-backup-";
        private readonly int maximumBackups;

        public BackupRecoveryService(int maximumBackups = 3)
        {
            if (maximumBackups < 1) throw new ArgumentOutOfRangeException(nameof(maximumBackups));
            this.maximumBackups = maximumBackups;
        }

        public void CreateBackup(ISaveRepository repository)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (InspectSaveHealth(repository) != SaveHealthStatus.Healthy)
                throw new InvalidOperationException("Only a healthy save can be backed up.");

            // Shift oldest first so slot zero is always the newest recovery point.
            repository.DeleteBackup(BackupId(maximumBackups - 1));
            for (var slot = maximumBackups - 1; slot > 0; slot--)
            {
                var source = BackupId(slot - 1);
                if (repository.HasBackup(source)) repository.CopyBackup(source, BackupId(slot));
            }
            repository.CreateBackup(BackupId(0));
        }

        /// <summary>Restores the newest available valid backup, preserving no-data saves.</summary>
        public bool RestoreFromBackup(ISaveRepository repository)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            for (var slot = 0; slot < maximumBackups; slot++)
            {
                var backupId = BackupId(slot);
                if (!repository.HasBackup(backupId)) continue;
                if (repository.RestoreBackup(backupId)) return true;
            }
            return false;
        }

        public SaveHealthStatus InspectSaveHealth(ISaveRepository repository)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (repository.IsCorrupted) return SaveHealthStatus.CorruptedNeedsRecovery;
            return repository.HasData ? SaveHealthStatus.Healthy : SaveHealthStatus.NoData;
        }

        private static string BackupId(int slot)
        {
            return BackupPrefix + slot.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
