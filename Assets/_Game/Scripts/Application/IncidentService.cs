using System;
using System.Collections.Generic;
using ChemistryLab.Domain;

namespace ChemistryLab.Application
{
    public enum IncidentSeverity
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        CriticalExplosion = 4
    }

    /// <summary>A UI-safe consequence proposal. Applying any penalty is owned by the caller.</summary>
    public sealed class IncidentResult
    {
        public IncidentResult(IncidentSeverity severity, long penaltyDollars, string penaltyMessage, string localisationKey)
        {
            Severity = severity;
            PenaltyDollars = penaltyDollars;
            PenaltyMessage = penaltyMessage;
            LocalisationKey = localisationKey;
        }

        public IncidentSeverity Severity { get; private set; }
        public long PenaltyDollars { get; private set; }
        public string PenaltyMessage { get; private set; }
        public string LocalisationKey { get; private set; }
    }

    /// <summary>
    /// Deterministic game-only incident rules. These thresholds model gameplay input,
    /// not real-world chemistry guidance.
    /// </summary>
    public sealed class IncidentService
    {
        public IncidentResult EvaluateRushedPour(Reaction reaction, decimal pourSpeedFactor)
        {
            if (reaction == null || pourSpeedFactor <= 1m)
            {
                return CreateResult(IncidentSeverity.None, 0L, "incident.rushed_pour.none");
            }

            if (pourSpeedFactor < 1.25m)
            {
                return CreateResult(IncidentSeverity.Low, 10L, "incident.rushed_pour.low");
            }

            if (pourSpeedFactor < 1.75m)
            {
                return CreateResult(IncidentSeverity.Medium, 25L, "incident.rushed_pour.medium");
            }

            if (pourSpeedFactor < 2.5m)
            {
                return CreateResult(IncidentSeverity.High, 50L, "incident.rushed_pour.high");
            }

            return CreateResult(IncidentSeverity.CriticalExplosion, 100L, "incident.rushed_pour.critical");
        }

        public IncidentResult EvaluateUncleanedExit(IEnumerable<ToolState> tools, PlayerProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var severityScore = 0;
            if (tools != null)
            {
                foreach (var tool in tools)
                {
                    if (tool == null || !tool.IsOwned)
                    {
                        continue;
                    }

                    if (tool.Cleanliness == ToolCleanState.Dirty)
                    {
                        severityScore++;
                    }
                    else if (tool.Cleanliness == ToolCleanState.NeedsWashing)
                    {
                        // Residue requiring a wash is more serious than a merely dirty tool.
                        severityScore += 2;
                    }

                    // Tools left on the bench or still in use count as the untidy-lab
                    // case from the incident rules, even if they are clean.
                    if (tool.StorageState == ToolStorageState.OnBench
                        || tool.StorageState == ToolStorageState.InUse)
                    {
                        severityScore++;
                    }
                }
            }

            IncidentSeverity severity;
            long basePenalty;
            string localisationKey;
            if (severityScore == 0)
            {
                severity = IncidentSeverity.None;
                basePenalty = 0L;
                localisationKey = "incident.uncleaned_exit.none";
            }
            else if (severityScore == 1)
            {
                severity = IncidentSeverity.Low;
                basePenalty = 10L;
                localisationKey = "incident.uncleaned_exit.low";
            }
            else if (severityScore == 2)
            {
                severity = IncidentSeverity.Medium;
                basePenalty = 25L;
                localisationKey = "incident.uncleaned_exit.medium";
            }
            else if (severityScore < 5)
            {
                severity = IncidentSeverity.High;
                basePenalty = 50L;
                localisationKey = "incident.uncleaned_exit.high";
            }
            else
            {
                severity = IncidentSeverity.CriticalExplosion;
                basePenalty = 120L;
                localisationKey = "incident.uncleaned_exit.critical";
            }

            var safeCap = CalculateSafePenaltyCap(profile);
            return CreateResult(severity, Math.Min(basePenalty, safeCap), localisationKey);
        }

        private static long CalculateSafePenaltyCap(PlayerProfile profile)
        {
            // Level-based cap keeps early-game cleanup incidents recoverable.
            return Math.Max(20L, (long)profile.Level * 25L + (long)profile.LabUpgradeLevel * 10L);
        }

        private static IncidentResult CreateResult(IncidentSeverity severity, long penaltyDollars, string localisationKey)
        {
            return new IncidentResult(severity, penaltyDollars, MessageFor(severity), localisationKey);
        }

        private static string MessageFor(IncidentSeverity severity)
        {
            switch (severity)
            {
                case IncidentSeverity.Low:
                    return "A small cleanup or safety penalty was applied.";
                case IncidentSeverity.Medium:
                    return "The lab needs additional cleanup; a moderate penalty was applied.";
                case IncidentSeverity.High:
                    return "Unsafe lab conditions caused a significant penalty.";
                case IncidentSeverity.CriticalExplosion:
                    return "Critical unsafe conditions triggered the maximum game incident penalty.";
                default:
                    return "No incident occurred.";
            }
        }
    }
}
