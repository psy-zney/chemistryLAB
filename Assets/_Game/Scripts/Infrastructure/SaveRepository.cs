using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using ChemistryLab.Application;
using ChemistryLab.Domain;

namespace ChemistryLab.Infrastructure
{
    /// <summary>
    /// Dependency-free file implementation of ISaveRepository. It is deliberately
    /// shaped like a database repository so a verified SQLite implementation can
    /// replace it later without changing application code.
    /// </summary>
    public sealed class SaveRepository : ISaveRepository
    {
        private const string Magic = "ChemistryLab.Save";
        private const int FormatVersion = 1;
        private readonly object syncRoot = new object();
        private readonly string savePath;
        private PlayerProfile profile;
        private Inventory inventory;
        private List<ToolState> tools;
        private List<ReactionTransaction> transactions;
        private bool corrupted;

        public SaveRepository(string saveFilePath)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                throw new ArgumentException("A save file path is required.", nameof(saveFilePath));
            }

            savePath = Path.GetFullPath(saveFilePath);
            inventory = new Inventory();
            tools = new List<ToolState>();
            transactions = new List<ReactionTransaction>();
            LoadFromDisk();
        }

        public bool HasData { get { return profile != null; } }
        public bool IsCorrupted { get { return corrupted; } }
        public PlayerProfile Profile { get { return profile; } }
        public Inventory Inventory { get { return inventory; } }
        public IList<ToolState> Tools { get { return tools; } }

        public PlayerProfile LoadProfile()
        {
            lock (syncRoot)
            {
                return CloneProfile(profile);
            }
        }

        public IReadOnlyDictionary<string, decimal> LoadInventory()
        {
            lock (syncRoot)
            {
                return new ReadOnlyDictionary<string, decimal>(CopyInventory(inventory));
            }
        }

        public IReadOnlyList<ToolState> LoadTools()
        {
            lock (syncRoot)
            {
                return CloneTools(tools).AsReadOnly();
            }
        }

        public IReadOnlyList<ReactionTransaction> LoadTransactions()
        {
            lock (syncRoot)
            {
                return CloneTransactions(transactions).AsReadOnly();
            }
        }

        public void SaveState(PlayerProfile newProfile, IDictionary<string, decimal> newInventory, IEnumerable<ToolState> newTools)
        {
            if (newProfile == null) throw new ArgumentNullException(nameof(newProfile));
            if (newInventory == null) throw new ArgumentNullException(nameof(newInventory));
            if (newTools == null) throw new ArgumentNullException(nameof(newTools));
            MutateAndPersist(delegate
            {
                profile = CloneProfile(newProfile);
                inventory = new Inventory(newInventory);
                tools = CloneTools(newTools);
            });
        }

        public void SaveProfile(PlayerProfile newProfile)
        {
            if (newProfile == null) throw new ArgumentNullException(nameof(newProfile));
            MutateAndPersist(delegate { profile = CloneProfile(newProfile); });
        }

        public void SaveInventory(IDictionary<string, decimal> newInventory)
        {
            if (newInventory == null) throw new ArgumentNullException(nameof(newInventory));
            MutateAndPersist(delegate { inventory = new Inventory(newInventory); });
        }

        public void SaveTools(IEnumerable<ToolState> newTools)
        {
            if (newTools == null) throw new ArgumentNullException(nameof(newTools));
            MutateAndPersist(delegate { tools = CloneTools(newTools); });
        }

        public bool CommitTransaction(ReactionTransaction transaction, Action applyMutations)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (applyMutations == null) throw new ArgumentNullException(nameof(applyMutations));

            lock (syncRoot)
            {
                if (corrupted) throw new InvalidOperationException("A corrupted save must be recovered before it can be changed.");
                if (ContainsIdempotencyKey(transaction.IdempotencyKey)) return false;

                var before = CaptureSnapshotUnsafe();
                try
                {
                    applyMutations();
                    transactions.Add(CloneTransaction(transaction));
                    PersistUnsafe();
                    return true;
                }
                catch
                {
                    RestoreSnapshotUnsafe(before);
                    throw;
                }
            }
        }

        public SaveSnapshot CaptureSnapshot()
        {
            lock (syncRoot) { return CaptureSnapshotUnsafe(); }
        }

        public void RestoreSnapshot(SaveSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            lock (syncRoot)
            {
                var before = CaptureSnapshotUnsafe();
                try
                {
                    RestoreSnapshotUnsafe(snapshot);
                    PersistUnsafe();
                }
                catch
                {
                    RestoreSnapshotUnsafe(before);
                    throw;
                }
            }
        }

        public void CreateBackup(string backupId)
        {
            ValidateBackupId(backupId);
            lock (syncRoot)
            {
                if (!HasData || corrupted) throw new InvalidOperationException("Only a healthy save can be backed up.");
                var backupPath = GetBackupPath(backupId);
                EnsureDirectoryExists(backupPath);
                File.Copy(savePath, backupPath, true);
            }
        }

        public bool RestoreBackup(string backupId)
        {
            ValidateBackupId(backupId);
            lock (syncRoot)
            {
                var backupPath = GetBackupPath(backupId);
                if (!File.Exists(backupPath)) return false;
                SaveSnapshot backup;
                try { backup = ReadSnapshot(backupPath); }
                catch (Exception exception)
                {
                    throw new InvalidDataException("The selected backup is not valid.", exception);
                }

                RestoreSnapshotUnsafe(backup);
                PersistUnsafe();
                corrupted = false;
                return true;
            }
        }

        public bool HasBackup(string backupId)
        {
            ValidateBackupId(backupId);
            return File.Exists(GetBackupPath(backupId));
        }

        public void CopyBackup(string sourceBackupId, string destinationBackupId)
        {
            ValidateBackupId(sourceBackupId);
            ValidateBackupId(destinationBackupId);
            var source = GetBackupPath(sourceBackupId);
            if (!File.Exists(source)) throw new FileNotFoundException("The source backup does not exist.", source);
            var destination = GetBackupPath(destinationBackupId);
            EnsureDirectoryExists(destination);
            File.Copy(source, destination, true);
        }

        public void DeleteBackup(string backupId)
        {
            ValidateBackupId(backupId);
            var backupPath = GetBackupPath(backupId);
            if (File.Exists(backupPath)) File.Delete(backupPath);
        }

        private void LoadFromDisk()
        {
            if (!File.Exists(savePath)) return;
            try
            {
                RestoreSnapshotUnsafe(ReadSnapshot(savePath));
            }
            catch
            {
                corrupted = true;
                profile = null;
                inventory = new Inventory();
                tools = new List<ToolState>();
                transactions = new List<ReactionTransaction>();
            }
        }

        private void MutateAndPersist(Action mutation)
        {
            lock (syncRoot)
            {
                if (corrupted) throw new InvalidOperationException("A corrupted save must be recovered before it can be changed.");
                var before = CaptureSnapshotUnsafe();
                try { mutation(); PersistUnsafe(); }
                catch { RestoreSnapshotUnsafe(before); throw; }
            }
        }

        private SaveSnapshot CaptureSnapshotUnsafe()
        {
            return new SaveSnapshot(CloneProfile(profile), CopyInventory(inventory), CloneTools(tools), CloneTransactions(transactions));
        }

        private void RestoreSnapshotUnsafe(SaveSnapshot snapshot)
        {
            profile = CloneProfile(snapshot.Profile);
            inventory = new Inventory(snapshot.Inventory ?? new Dictionary<string, decimal>(StringComparer.Ordinal));
            tools = CloneTools(snapshot.Tools ?? new ToolState[0]);
            transactions = CloneTransactions(snapshot.Transactions ?? new ReactionTransaction[0]);
        }

        private void PersistUnsafe()
        {
            if (profile == null) throw new InvalidOperationException("A player profile is required before saving.");
            var bytes = SerializeSnapshot(CaptureSnapshotUnsafe());
            EnsureDirectoryExists(savePath);
            var temporaryPath = savePath + ".tmp";
            File.WriteAllBytes(temporaryPath, bytes);
            try
            {
                if (File.Exists(savePath)) File.Replace(temporaryPath, savePath, null);
                else File.Move(temporaryPath, savePath);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(temporaryPath, savePath, true);
                File.Delete(temporaryPath);
            }
            corrupted = false;
        }

        private bool ContainsIdempotencyKey(string key)
        {
            for (var index = 0; index < transactions.Count; index++)
            {
                if (string.Equals(transactions[index].IdempotencyKey, key, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private string GetBackupPath(string backupId) { return savePath + ".backup." + backupId; }
        private static void ValidateBackupId(string backupId)
        {
            if (string.IsNullOrWhiteSpace(backupId) || backupId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("A file-name-safe backup id is required.", nameof(backupId));
        }
        private static void EnsureDirectoryExists(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        }

        private static byte[] SerializeSnapshot(SaveSnapshot snapshot)
        {
            byte[] payload;
            using (var payloadStream = new MemoryStream())
            using (var writer = new BinaryWriter(payloadStream))
            {
                writer.Write(FormatVersion);
                WriteProfile(writer, snapshot.Profile);
                WriteInventory(writer, snapshot.Inventory);
                WriteTools(writer, snapshot.Tools);
                WriteTransactions(writer, snapshot.Transactions);
                writer.Flush();
                payload = payloadStream.ToArray();
            }
            byte[] hash;
            using (var sha = SHA256.Create()) { hash = sha.ComputeHash(payload); }
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(payload.Length);
                writer.Write(payload);
                writer.Write(hash.Length);
                writer.Write(hash);
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static SaveSnapshot ReadSnapshot(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                if (!string.Equals(reader.ReadString(), Magic, StringComparison.Ordinal)) throw new InvalidDataException("Unknown save format.");
                var payloadLength = reader.ReadInt32();
                if (payloadLength < 1 || payloadLength > stream.Length) throw new InvalidDataException("Invalid save payload length.");
                var payload = reader.ReadBytes(payloadLength);
                var hashLength = reader.ReadInt32();
                var storedHash = reader.ReadBytes(hashLength);
                if (storedHash.Length != hashLength || stream.Position != stream.Length) throw new InvalidDataException("Incomplete save checksum.");
                byte[] actualHash;
                using (var sha = SHA256.Create()) { actualHash = sha.ComputeHash(payload); }
                if (!HashesEqual(storedHash, actualHash)) throw new InvalidDataException("Save checksum does not match.");
                using (var payloadStream = new MemoryStream(payload))
                using (var payloadReader = new BinaryReader(payloadStream))
                {
                    if (payloadReader.ReadInt32() != FormatVersion) throw new InvalidDataException("Unsupported save format version.");
                    var snapshot = new SaveSnapshot(ReadProfile(payloadReader), ReadInventory(payloadReader), ReadTools(payloadReader), ReadTransactions(payloadReader));
                    if (payloadStream.Position != payloadStream.Length) throw new InvalidDataException("Unexpected save payload data.");
                    return snapshot;
                }
            }
        }

        private static void WriteProfile(BinaryWriter writer, PlayerProfile value)
        {
            writer.Write(value != null);
            if (value == null) return;
            writer.Write(value.PlayerId); writer.Write(value.Dollars); writer.Write(value.Diamonds); writer.Write(value.Level); writer.Write(value.Exp);
            writer.Write(value.LabUpgradeLevel); writer.Write(value.ActiveLabId ?? string.Empty); writer.Write(value.SchemaVersion);
            WriteDate(writer, value.CreatedAt); WriteDate(writer, value.UpdatedAt);
        }
        private static PlayerProfile ReadProfile(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            var playerId = reader.ReadString(); var dollars = reader.ReadInt64(); var diamonds = reader.ReadInt64(); var level = reader.ReadInt32(); var exp = reader.ReadInt32();
            var labLevel = reader.ReadInt32(); var labId = reader.ReadString(); var schema = reader.ReadInt32(); var created = ReadDate(reader); var updated = ReadDate(reader);
            return new PlayerProfile(playerId, dollars, diamonds, level, exp, labLevel, string.IsNullOrEmpty(labId) ? null : labId, schema, created, updated);
        }
        private static void WriteInventory(BinaryWriter writer, IDictionary<string, decimal> values)
        {
            var copy = values == null ? new Dictionary<string, decimal>() : new Dictionary<string, decimal>(values, StringComparer.Ordinal);
            writer.Write(copy.Count); foreach (var item in copy) { writer.Write(item.Key); writer.Write(item.Value); }
        }
        private static Dictionary<string, decimal> ReadInventory(BinaryReader reader)
        {
            var count = ReadCount(reader); var result = new Dictionary<string, decimal>(StringComparer.Ordinal);
            for (var i = 0; i < count; i++) result.Add(reader.ReadString(), reader.ReadDecimal());
            return result;
        }
        private static void WriteTools(BinaryWriter writer, IEnumerable<ToolState> values)
        {
            var copy = CloneTools(values ?? new ToolState[0]); writer.Write(copy.Count);
            foreach (var value in copy) { writer.Write(value.ToolId); writer.Write(value.IsOwned); writer.Write((int)value.Cleanliness); writer.Write((int)value.StorageState); }
        }
        private static List<ToolState> ReadTools(BinaryReader reader)
        {
            var count = ReadCount(reader); var result = new List<ToolState>(count);
            for (var i = 0; i < count; i++) result.Add(new ToolState(reader.ReadString(), reader.ReadBoolean(), (ToolCleanState)reader.ReadInt32(), (ToolStorageState)reader.ReadInt32()));
            return result;
        }
        private static void WriteTransactions(BinaryWriter writer, IEnumerable<ReactionTransaction> values)
        {
            var copy = CloneTransactions(values ?? new ReactionTransaction[0]); writer.Write(copy.Count);
            foreach (var value in copy)
            {
                writer.Write(value.TransactionId); writer.Write((int)value.Kind); WriteInventory(writer, ToDictionary(value.Inputs)); WriteInventory(writer, ToDictionary(value.Outputs));
                writer.Write(value.DollarDelta); writer.Write(value.DiamondDelta); writer.Write(value.IdempotencyKey); WriteDate(writer, value.Timestamp);
            }
        }
        private static List<ReactionTransaction> ReadTransactions(BinaryReader reader)
        {
            var count = ReadCount(reader); var result = new List<ReactionTransaction>(count);
            for (var i = 0; i < count; i++)
                result.Add(new ReactionTransaction(reader.ReadString(), (ReactionTransactionKind)reader.ReadInt32(), ReadInventory(reader), ReadInventory(reader), reader.ReadInt64(), reader.ReadInt64(), reader.ReadString(), ReadDate(reader)));
            return result;
        }
        private static void WriteDate(BinaryWriter writer, DateTimeOffset value) { writer.Write(value.Ticks); writer.Write((short)value.Offset.TotalMinutes); }
        private static DateTimeOffset ReadDate(BinaryReader reader) { return new DateTimeOffset(reader.ReadInt64(), TimeSpan.FromMinutes(reader.ReadInt16())); }
        private static int ReadCount(BinaryReader reader) { var value = reader.ReadInt32(); if (value < 0 || value > 100000) throw new InvalidDataException("Invalid collection count."); return value; }
        private static bool HashesEqual(byte[] first, byte[] second) { if (first.Length != second.Length) return false; var difference = 0; for (var i = 0; i < first.Length; i++) difference |= first[i] ^ second[i]; return difference == 0; }
        private static PlayerProfile CloneProfile(PlayerProfile value)
        {
            return value == null ? null : new PlayerProfile(value.PlayerId, value.Dollars, value.Diamonds, value.Level, value.Exp, value.LabUpgradeLevel, value.ActiveLabId, value.SchemaVersion, value.CreatedAt, value.UpdatedAt);
        }
        private static Dictionary<string, decimal> CopyInventory(Inventory value) { return value == null ? new Dictionary<string, decimal>(StringComparer.Ordinal) : ToDictionary(value.Quantities); }
        private static Dictionary<string, decimal> ToDictionary(IReadOnlyDictionary<string, decimal> value) { var result = new Dictionary<string, decimal>(StringComparer.Ordinal); if (value != null) foreach (var item in value) result.Add(item.Key, item.Value); return result; }
        private static List<ToolState> CloneTools(IEnumerable<ToolState> values) { var result = new List<ToolState>(); if (values != null) foreach (var value in values) { if (value == null) throw new ArgumentException("Tool state entries cannot be null."); result.Add(new ToolState(value.ToolId, value.IsOwned, value.Cleanliness, value.StorageState)); } return result; }
        private static List<ReactionTransaction> CloneTransactions(IEnumerable<ReactionTransaction> values) { var result = new List<ReactionTransaction>(); if (values != null) foreach (var value in values) { if (value == null) throw new ArgumentException("Transaction entries cannot be null."); result.Add(CloneTransaction(value)); } return result; }
        private static ReactionTransaction CloneTransaction(ReactionTransaction value) { return new ReactionTransaction(value.TransactionId, value.Kind, ToDictionary(value.Inputs), ToDictionary(value.Outputs), value.DollarDelta, value.DiamondDelta, value.IdempotencyKey, value.Timestamp); }
    }
}
