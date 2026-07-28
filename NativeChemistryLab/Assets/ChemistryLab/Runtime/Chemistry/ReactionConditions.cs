using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    public enum ReactionRateClass
    {
        Stopped,
        VerySlow,
        Slow,
        Moderate,
        Fast,
        Vigorous
    }

    /// <summary>
    /// Trạng thái vật lý của một bình phản ứng. Nhiệt độ và thể tích tồn tại độc lập
    /// với danh sách chất, nhờ đó người chơi có thể gia nhiệt hoặc pha loãng trước
    /// khi phản ứng được phép xảy ra.
    /// </summary>
    [Serializable]
    public sealed class ReactionEnvironment
    {
        public const float MinimumTemperatureC = -20f;
        public const float MaximumTemperatureC = 250f;
        public const double MinimumVolumeLitres = 0.010d;
        public const double MaximumVolumeLitres = 2.000d;

        public ReactionEnvironment(float temperatureC, double volumeLitres)
        {
            TemperatureC = Mathf.Clamp(
                temperatureC,
                MinimumTemperatureC,
                MaximumTemperatureC);
            VolumeLitres = Math.Max(
                MinimumVolumeLitres,
                Math.Min(MaximumVolumeLitres, volumeLitres));
        }

        public float TemperatureC { get; private set; }
        public double VolumeLitres { get; private set; }

        public void ChangeTemperature(float deltaC)
        {
            TemperatureC = Mathf.Clamp(
                TemperatureC + deltaC,
                MinimumTemperatureC,
                MaximumTemperatureC);
        }

        public void Dilute(double addedLitres)
        {
            VolumeLitres = Math.Max(
                MinimumVolumeLitres,
                Math.Min(MaximumVolumeLitres, VolumeLitres + Math.Max(0d, addedLitres)));
        }

        public void Reset(float temperatureC = 24f, double volumeLitres = 0.100d)
        {
            TemperatureC = Mathf.Clamp(
                temperatureC,
                MinimumTemperatureC,
                MaximumTemperatureC);
            VolumeLitres = Math.Max(
                MinimumVolumeLitres,
                Math.Min(MaximumVolumeLitres, volumeLitres));
        }

        public ReactionEnvironment Clone()
        {
            return new ReactionEnvironment(TemperatureC, VolumeLitres);
        }
    }

    public sealed class ReactionConditionProfile
    {
        public string ReactionId;
        public float MinimumTemperatureC = float.NegativeInfinity;
        public float MaximumTemperatureC = float.PositiveInfinity;
        public double MinimumPH = double.NegativeInfinity;
        public double MaximumPH = double.PositiveInfinity;
        public string ConcentrationChemicalId;
        public double MinimumConcentrationMolar;
        public double MaximumConcentrationMolar = double.PositiveInfinity;
        public string RequiredCatalystId;
        public bool CatalystIsReactionParticipant;
        public float BaseCompletionSeconds = 12f;
    }

    public sealed class ReactionConditionAssessment
    {
        public bool ConditionsMet;
        public double PH;
        public double TotalConcentrationMolar;
        public double FocusConcentrationMolar;
        public ReactionRateClass RateClass;
        public float RateMultiplier;
        public float YieldMultiplier;
        public float EstimatedCompletionSeconds;
        public string CatalystSummary;
        public string Summary;
        public string BlockingReason;
    }

    /// <summary>
    /// Educational kinetics/condition layer. It intentionally models trends rather
    /// than laboratory-grade thermodynamics: conservation and stoichiometry remain
    /// in ReactionSimulator, while this class owns pH, concentration, catalyst and rate.
    /// </summary>
    public static class ReactionConditionEngine
    {
        private sealed class AcidBaseContribution
        {
            public AcidBaseContribution(double equivalents, double dissociation)
            {
                Equivalents = equivalents;
                Dissociation = dissociation;
            }

            public double Equivalents;
            public double Dissociation;
        }

        private static readonly Dictionary<string, AcidBaseContribution> Acids =
            new Dictionary<string, AcidBaseContribution>(StringComparer.Ordinal)
            {
                { "hydrochloric-acid", new AcidBaseContribution(1d, 1d) },
                { "nitric-acid", new AcidBaseContribution(1d, 1d) },
                { "sulfuric-acid", new AcidBaseContribution(2d, .75d) },
                { "phosphoric-acid", new AcidBaseContribution(3d, .08d) },
                { "acetic-acid", new AcidBaseContribution(1d, .012d) }
            };

        private static readonly Dictionary<string, AcidBaseContribution> Bases =
            new Dictionary<string, AcidBaseContribution>(StringComparer.Ordinal)
            {
                { "sodium-hydroxide", new AcidBaseContribution(1d, 1d) },
                { "potassium-hydroxide", new AcidBaseContribution(1d, 1d) },
                { "calcium-hydroxide", new AcidBaseContribution(2d, .75d) },
                { "barium-hydroxide", new AcidBaseContribution(2d, 1d) },
                { "ammonia", new AcidBaseContribution(1d, .012d) }
            };

        private static readonly Dictionary<string, ReactionConditionProfile> Profiles =
            BuildProfiles();

        public static int ProfileCount
        {
            get { return Profiles.Count; }
        }

        public static ReactionConditionAssessment Assess(
            ReactionDefinition reaction,
            IReadOnlyDictionary<string, double> gramsById,
            ReactionEnvironment environment)
        {
            environment = environment ?? new ReactionEnvironment(24f, .100d);
            var profile = GetProfile(reaction);
            var pH = EstimatePH(gramsById, environment.VolumeLitres);
            var totalConcentration = EstimateTotalConcentration(gramsById, environment.VolumeLitres);
            var focusConcentration = profile == null
                ? 0d
                : EstimateConcentration(
                    profile.ConcentrationChemicalId,
                    gramsById,
                    environment.VolumeLitres);
            var blockers = new List<string>();
            var catalystPresent = profile == null
                || string.IsNullOrWhiteSpace(profile.RequiredCatalystId)
                || HasPositiveMass(profile.RequiredCatalystId, gramsById);

            if (profile != null)
            {
                if (environment.TemperatureC < profile.MinimumTemperatureC)
                {
                    blockers.Add(
                        "cần ≥ " + profile.MinimumTemperatureC.ToString("0.#") + " °C");
                }

                if (environment.TemperatureC > profile.MaximumTemperatureC)
                {
                    blockers.Add(
                        "cần ≤ " + profile.MaximumTemperatureC.ToString("0.#") + " °C");
                }

                if (pH < profile.MinimumPH)
                {
                    blockers.Add("cần pH ≥ " + profile.MinimumPH.ToString("0.#"));
                }

                if (pH > profile.MaximumPH)
                {
                    blockers.Add("cần pH ≤ " + profile.MaximumPH.ToString("0.#"));
                }

                if (!string.IsNullOrWhiteSpace(profile.ConcentrationChemicalId)
                    && focusConcentration < profile.MinimumConcentrationMolar)
                {
                    blockers.Add(
                        "nồng độ " + FormulaFor(profile.ConcentrationChemicalId)
                        + " cần ≥ " + profile.MinimumConcentrationMolar.ToString("0.##") + " M");
                }

                if (!string.IsNullOrWhiteSpace(profile.ConcentrationChemicalId)
                    && focusConcentration > profile.MaximumConcentrationMolar)
                {
                    blockers.Add(
                        "nồng độ " + FormulaFor(profile.ConcentrationChemicalId)
                        + " cần ≤ " + profile.MaximumConcentrationMolar.ToString("0.##") + " M");
                }

                if (!catalystPresent)
                {
                    blockers.Add("thiếu xúc tác " + FormulaFor(profile.RequiredCatalystId));
                }
            }

            var temperatureFactor = Math.Exp((environment.TemperatureC - 25d) / 42d);
            temperatureFactor = Clamp(temperatureFactor, .10d, 4.5d);
            var concentrationFactor = Math.Sqrt(Math.Max(.02d, totalConcentration));
            concentrationFactor = Clamp(concentrationFactor, .20d, 2.2d);
            var catalystFactor = profile != null
                && !string.IsNullOrWhiteSpace(profile.RequiredCatalystId)
                && catalystPresent
                    ? 2.4d
                    : 1d;
            var rateMultiplier = (float)Clamp(
                temperatureFactor * concentrationFactor * catalystFactor,
                .05d,
                8d);
            var rateClass = blockers.Count > 0
                ? ReactionRateClass.Stopped
                : ClassifyRate(rateMultiplier);
            var baseSeconds = profile == null ? 12f : profile.BaseCompletionSeconds;
            var completionSeconds = blockers.Count > 0
                ? 0f
                : Mathf.Clamp(baseSeconds / Mathf.Max(.05f, rateMultiplier), .5f, 240f);
            var yieldMultiplier = blockers.Count > 0
                ? 0f
                : Mathf.Clamp(.72f + .18f * Mathf.Log10(1f + rateMultiplier * 4f), .55f, 1f);
            var catalystSummary = profile != null && !string.IsNullOrWhiteSpace(profile.RequiredCatalystId)
                ? FormulaFor(profile.RequiredCatalystId) + (catalystPresent ? " · có mặt, không tiêu hao" : " · chưa có")
                : "Không yêu cầu xúc tác";

            var summary = new StringBuilder();
            summary.Append(environment.TemperatureC.ToString("0.#")).Append(" °C · ")
                .Append((environment.VolumeLitres * 1000d).ToString("0")).Append(" mL · pH ")
                .Append(pH.ToString("0.00")).Append(" · tổng ")
                .Append(totalConcentration.ToString("0.000")).Append(" M · ")
                .Append(RateLabel(rateClass));

            return new ReactionConditionAssessment
            {
                ConditionsMet = blockers.Count == 0,
                PH = pH,
                TotalConcentrationMolar = totalConcentration,
                FocusConcentrationMolar = focusConcentration,
                RateClass = rateClass,
                RateMultiplier = blockers.Count > 0 ? 0f : rateMultiplier,
                YieldMultiplier = yieldMultiplier,
                EstimatedCompletionSeconds = completionSeconds,
                CatalystSummary = catalystSummary,
                Summary = summary.ToString(),
                BlockingReason = blockers.Count == 0
                    ? string.Empty
                    : "Điều kiện chưa đạt: " + string.Join("; ", blockers.ToArray()) + "."
            };
        }

        public static bool IsCatalystParticipant(ReactionDefinition reaction, string chemicalId)
        {
            var profile = GetProfile(reaction);
            return profile != null
                && profile.CatalystIsReactionParticipant
                && string.Equals(profile.RequiredCatalystId, chemicalId, StringComparison.Ordinal);
        }

        public static double EstimatePH(
            IReadOnlyDictionary<string, double> gramsById,
            double volumeLitres)
        {
            if (gramsById == null || gramsById.Count == 0)
            {
                return 7d;
            }

            volumeLitres = Math.Max(ReactionEnvironment.MinimumVolumeLitres, volumeLitres);
            var acidEquivalents = 0d;
            var baseEquivalents = 0d;
            foreach (var pair in gramsById)
            {
                var chemical = RuntimeChemicalRegistry.GetChemical(pair.Key);
                if (chemical == null || chemical.MolarMass <= 0d || pair.Value <= 0d)
                {
                    continue;
                }

                var moles = pair.Value / chemical.MolarMass;
                AcidBaseContribution contribution;
                if (Acids.TryGetValue(pair.Key, out contribution))
                {
                    acidEquivalents += moles * contribution.Equivalents * contribution.Dissociation;
                }
                else if (Bases.TryGetValue(pair.Key, out contribution))
                {
                    baseEquivalents += moles * contribution.Equivalents * contribution.Dissociation;
                }
            }

            var net = acidEquivalents - baseEquivalents;
            if (Math.Abs(net) < 1e-10d)
            {
                return 7d;
            }

            if (net > 0d)
            {
                var hydrogen = Math.Max(1e-14d, net / volumeLitres);
                return Clamp(-Math.Log10(hydrogen), 0d, 14d);
            }

            var hydroxide = Math.Max(1e-14d, -net / volumeLitres);
            return Clamp(14d + Math.Log10(hydroxide), 0d, 14d);
        }

        public static double EstimateConcentration(
            string chemicalId,
            IReadOnlyDictionary<string, double> gramsById,
            double volumeLitres)
        {
            if (string.IsNullOrWhiteSpace(chemicalId) || gramsById == null)
            {
                return 0d;
            }

            double grams;
            var chemical = RuntimeChemicalRegistry.GetChemical(chemicalId);
            return chemical != null
                && chemical.MolarMass > 0d
                && gramsById.TryGetValue(chemicalId, out grams)
                    ? Math.Max(0d, grams) / chemical.MolarMass
                      / Math.Max(ReactionEnvironment.MinimumVolumeLitres, volumeLitres)
                    : 0d;
        }

        public static double EstimateTotalConcentration(
            IReadOnlyDictionary<string, double> gramsById,
            double volumeLitres)
        {
            if (gramsById == null)
            {
                return 0d;
            }

            var moles = 0d;
            foreach (var pair in gramsById)
            {
                var chemical = RuntimeChemicalRegistry.GetChemical(pair.Key);
                if (chemical != null && chemical.MolarMass > 0d)
                {
                    moles += Math.Max(0d, pair.Value) / chemical.MolarMass;
                }
            }

            return moles / Math.Max(ReactionEnvironment.MinimumVolumeLitres, volumeLitres);
        }

        public static void ValidateOrThrow()
        {
            var environment = new ReactionEnvironment(24f, .100d);
            var hydrochloric = RuntimeChemicalRegistry.GetChemical("hydrochloric-acid");
            var sodiumHydroxide = RuntimeChemicalRegistry.GetChemical("sodium-hydroxide");
            var neutral = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                { "hydrochloric-acid", hydrochloric.MolarMass * .1d },
                { "sodium-hydroxide", sodiumHydroxide.MolarMass * .1d }
            };
            var neutralPH = EstimatePH(neutral, environment.VolumeLitres);
            if (Math.Abs(neutralPH - 7d) > .1d)
            {
                throw new InvalidOperationException("Reaction condition pH neutralisation validation failed.");
            }

            var peroxide = DesktopChemistryDatabase.GetReaction("peroxide-decomposition");
            var missingCatalyst = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                { "hydrogen-peroxide", 6.803d }
            };
            if (Assess(peroxide, missingCatalyst, environment).ConditionsMet)
            {
                throw new InvalidOperationException("Catalyst requirement validation failed.");
            }

            missingCatalyst["manganese-dioxide"] = .2d;
            if (!Assess(peroxide, missingCatalyst, environment).ConditionsMet)
            {
                throw new InvalidOperationException("Catalysed reaction condition validation failed.");
            }
        }

        private static ReactionConditionProfile GetProfile(ReactionDefinition reaction)
        {
            if (reaction == null)
            {
                return null;
            }

            ReactionConditionProfile profile;
            return Profiles.TryGetValue(reaction.Id, out profile) ? profile : null;
        }

        private static Dictionary<string, ReactionConditionProfile> BuildProfiles()
        {
            var result = new Dictionary<string, ReactionConditionProfile>(StringComparer.Ordinal)
            {
                {
                    "peroxide-decomposition",
                    new ReactionConditionProfile
                    {
                        ReactionId = "peroxide-decomposition",
                        MinimumTemperatureC = 15f,
                        RequiredCatalystId = "manganese-dioxide",
                        CatalystIsReactionParticipant = true,
                        BaseCompletionSeconds = 18f
                    }
                },
                {
                    "redox-permanganate-peroxide",
                    Acidic("redox-permanganate-peroxide", 3d, 16f)
                },
                {
                    "redox-permanganate-chloride",
                    Acidic("redox-permanganate-chloride", 2.5d, 18f)
                },
                {
                    "redox-permanganate-iodide",
                    Acidic("redox-permanganate-iodide", 3d, 14f)
                },
                {
                    "redox-peroxide-iodide",
                    Acidic("redox-peroxide-iodide", 3d, 16f)
                },
                {
                    "redox-permanganate-iron-two",
                    Acidic("redox-permanganate-iron-two", 3d, 14f)
                },
                {
                    "redox-copper-sulfuric",
                    new ReactionConditionProfile
                    {
                        ReactionId = "redox-copper-sulfuric",
                        MinimumTemperatureC = 70f,
                        MaximumPH = 1.5d,
                        ConcentrationChemicalId = "sulfuric-acid",
                        MinimumConcentrationMolar = 3d,
                        BaseCompletionSeconds = 30f
                    }
                }
            };

            return result;
        }

        private static ReactionConditionProfile Acidic(
            string reactionId,
            double maximumPH,
            float seconds)
        {
            return new ReactionConditionProfile
            {
                ReactionId = reactionId,
                MaximumPH = maximumPH,
                BaseCompletionSeconds = seconds
            };
        }

        private static bool HasPositiveMass(
            string chemicalId,
            IReadOnlyDictionary<string, double> gramsById)
        {
            double grams;
            return gramsById != null
                && gramsById.TryGetValue(chemicalId, out grams)
                && grams > 0d;
        }

        private static ReactionRateClass ClassifyRate(float multiplier)
        {
            if (multiplier < .18f) return ReactionRateClass.VerySlow;
            if (multiplier < .55f) return ReactionRateClass.Slow;
            if (multiplier < 1.35f) return ReactionRateClass.Moderate;
            if (multiplier < 3f) return ReactionRateClass.Fast;
            return ReactionRateClass.Vigorous;
        }

        private static string RateLabel(ReactionRateClass rate)
        {
            switch (rate)
            {
                case ReactionRateClass.Stopped: return "bị chặn";
                case ReactionRateClass.VerySlow: return "rất chậm";
                case ReactionRateClass.Slow: return "chậm";
                case ReactionRateClass.Fast: return "nhanh";
                case ReactionRateClass.Vigorous: return "mãnh liệt";
                default: return "vừa";
            }
        }

        private static string FormulaFor(string chemicalId)
        {
            var chemical = RuntimeChemicalRegistry.GetChemical(chemicalId);
            return chemical == null ? chemicalId : chemical.Formula;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
