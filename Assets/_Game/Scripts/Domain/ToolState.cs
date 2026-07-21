using System;

namespace ChemistryLab.Domain
{
    public enum ToolStorageState
    {
        Stored = 0,
        OnBench = 1,
        InUse = 2
    }

    /// <summary>Player-specific ownership, cleanliness, and location of a catalogue tool.</summary>
    public sealed class ToolState
    {
        public ToolState(string toolId, bool isOwned, ToolCleanState cleanliness, ToolStorageState storageState)
        {
            ToolId = RequireText(toolId, nameof(toolId));
            IsOwned = isOwned;
            Cleanliness = cleanliness;
            StorageState = storageState;
        }

        public string ToolId { get; private set; }
        public bool IsOwned { get; private set; }
        public ToolCleanState Cleanliness { get; private set; }
        public ToolStorageState StorageState { get; private set; }

        public void SetOwned(bool isOwned)
        {
            IsOwned = isOwned;
            if (!isOwned)
            {
                StorageState = ToolStorageState.Stored;
            }
        }

        public void SetCleanliness(ToolCleanState cleanliness)
        {
            Cleanliness = cleanliness;
        }

        public bool TrySetStorageState(ToolStorageState storageState)
        {
            if (!IsOwned && storageState != ToolStorageState.Stored)
            {
                return false;
            }

            StorageState = storageState;
            return true;
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }
    }
}
