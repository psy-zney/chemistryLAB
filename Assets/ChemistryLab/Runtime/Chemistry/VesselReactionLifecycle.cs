using System;
using System.Collections.Generic;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// One bounded reaction per vessel between cleanups. Evaluate remains a pure prediction;
    /// only Advance commits it. Input mass is bookkeeping, not a full solvent/byproduct model.
    /// </summary>
    public sealed class VesselReactionLifecycle
    {
        public ReactionOutcome CommittedOutcome { get; private set; }
        public bool RequiresCleanup { get { return CommittedOutcome != null; } }
        public bool CanCollect
        {
            get
            {
                return CommittedOutcome != null && !CommittedOutcome.ProductCollected
                    && CommittedOutcome.CanCollectProduct;
            }
        }

        public ReactionOutcome Preview(IReadOnlyList<VesselAddition> additions,
            LabStation station, ReactionEnvironment environment)
        {
            return CommittedOutcome ?? ReactionSimulator.Evaluate(additions, station, environment);
        }

        public ReactionOutcome Advance(IReadOnlyList<VesselAddition> additions,
            LabStation station, ReactionEnvironment environment, out bool committedNow)
        {
            committedNow = false;
            var outcome = Preview(additions, station, environment);
            if (CommittedOutcome != null || outcome.Status != ReactionStatus.Reaction)
            {
                return outcome;
            }

            CommittedOutcome = outcome;
            outcome.ReactionCommitted = true;
            for (var index = 0; index < additions.Count; index++)
            {
                outcome.RecordedInputGrams += Math.Max(0d, additions[index].Grams);
            }
            outcome.UnallocatedInputGrams = outcome.RecordedInputGrams;
            committedNow = true;
            return outcome;
        }

        public bool MarkCollected()
        {
            if (!CanCollect)
            {
                return false;
            }
            CommittedOutcome.ProductCollected = true;
            CommittedOutcome.CanCollectProduct = false;
            CommittedOutcome.CollectedProductGrams = CommittedOutcome.EstimatedProductGrams;
            // Signed difference deliberately exposes an incomplete model instead of clamping
            // away a mass discrepancy. Initial solvent and product speciation are not tracked.
            CommittedOutcome.UnallocatedInputGrams = CommittedOutcome.RecordedInputGrams
                - CommittedOutcome.CollectedProductGrams;
            return true;
        }

        public void Reset()
        {
            CommittedOutcome = null;
        }

        public static void ValidateOrThrow()
        {
            var additions = new List<VesselAddition>
            {
                new VesselAddition("copper-sulfate", 10d),
                new VesselAddition("sodium-hydroxide", 10d)
            };
            var environment = new ReactionEnvironment(24f, .100d);
            var lifecycle = new VesselReactionLifecycle();
            bool committed;
            var first = lifecycle.Advance(additions, LabStation.Workbench, environment, out committed);
            if (!committed || !lifecycle.CanCollect || first.RecordedInputGrams != 20d)
            {
                throw new InvalidOperationException("Vessel reaction commitment validation failed.");
            }
            environment.ChangeTemperature(25f);
            var second = lifecycle.Advance(additions, LabStation.Workbench, environment, out committed);
            if (committed || !ReferenceEquals(first, second) || !lifecycle.MarkCollected()
                || lifecycle.MarkCollected() || lifecycle.CanCollect || !lifecycle.RequiresCleanup
                || Math.Abs(first.UnallocatedInputGrams + first.CollectedProductGrams - 20d) > .000001d)
            {
                throw new InvalidOperationException("Vessel replay/collection accounting validation failed.");
            }
            lifecycle.Advance(additions, LabStation.Workbench, environment, out committed);
            if (committed || lifecycle.CanCollect)
            {
                throw new InvalidOperationException("Collected reaction was replayed.");
            }
            lifecycle.Reset();
            additions.Clear();
            var empty = lifecycle.Advance(additions, LabStation.Workbench, environment, out committed);
            if (committed || lifecycle.RequiresCleanup || empty.Status != ReactionStatus.Idle)
            {
                throw new InvalidOperationException("Vessel cleanup validation failed.");
            }
            additions.Add(new VesselAddition("hydrogen-peroxide", 6.803d));
            additions.Add(new VesselAddition("manganese-dioxide", .2d));
            environment.Reset(0f, .100d);
            var blocked = lifecycle.Advance(additions, LabStation.Workbench, environment, out committed);
            if (committed || blocked.Status != ReactionStatus.Blocked || lifecycle.CanCollect)
            {
                throw new InvalidOperationException("Blocked vessel committed prematurely.");
            }
            environment.ChangeTemperature(25f);
            var heated = lifecycle.Advance(additions, LabStation.Workbench, environment, out committed);
            if (!committed || heated.Status != ReactionStatus.Reaction || !lifecycle.CanCollect)
            {
                throw new InvalidOperationException("Heating did not commit the newly possible reaction.");
            }
            environment.Dilute(.050d);
            lifecycle.Advance(additions, LabStation.Workbench, environment, out committed);
            if (committed)
            {
                throw new InvalidOperationException("Dilution replayed a committed reaction.");
            }
        }
    }
}
