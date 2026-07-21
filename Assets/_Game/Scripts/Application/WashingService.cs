using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Application
{
    public enum WashingStatus
    {
        Washed = 0,
        AlreadyClean = 1,
        InvalidTool = 2,
        InvalidProfile = 3
    }

    public sealed class WashingResult
    {
        internal WashingResult(WashingStatus status, string message, string localisationKey)
        {
            Status = status;
            Message = message;
            LocalisationKey = localisationKey;
        }

        public WashingStatus Status { get; private set; }
        public string Message { get; private set; }
        public string LocalisationKey { get; private set; }
    }

    /// <summary>Domain service for cleaning player-owned laboratory tools at a sink.</summary>
    public sealed class WashingService
    {
        public WashingResult WashTool(ToolState tool, PlayerProfile profile)
        {
            if (profile == null)
            {
                return new WashingResult(WashingStatus.InvalidProfile, "A player profile is required.", "tool.wash.invalid_profile");
            }

            if (tool == null || !tool.IsOwned)
            {
                return new WashingResult(WashingStatus.InvalidTool, "Only an owned tool can be washed.", "tool.wash.invalid_tool");
            }

            if (tool.Cleanliness == ToolCleanState.Clean)
            {
                return new WashingResult(WashingStatus.AlreadyClean, "The tool is already clean.", "tool.wash.already_clean");
            }

            tool.SetCleanliness(ToolCleanState.Clean);
            return new WashingResult(WashingStatus.Washed, "Tool washed successfully.", "tool.wash.success");
        }

        /// <summary>Returns owned tools that still require attention before leaving the lab.</summary>
        public IReadOnlyList<ToolState> CheckUnwashedExitWarning(IEnumerable<ToolState> allTools)
        {
            var unwashedTools = new List<ToolState>();
            if (allTools == null)
            {
                return unwashedTools.AsReadOnly();
            }

            foreach (var tool in allTools)
            {
                if (tool != null && tool.IsOwned && tool.Cleanliness != ToolCleanState.Clean)
                {
                    unwashedTools.Add(tool);
                }
            }

            return unwashedTools.AsReadOnly();
        }
    }
}
