using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// Data-driven X/Y/Z compound generator.
    ///
    /// X: metal/cation and activity.
    /// Y: nonmetal or reusable acid-residue anion.
    /// Z: oxygen count / oxidation state, represented explicitly by oxyanions
    ///    and oxide generation.
    ///
    /// The class generates candidates, balances charge, estimates coarse physical
    /// properties and rejects explicitly unstable combinations. Reaction rules
    /// remain a separate concern in DynamicReactionEngine.
    /// </summary>
    public static class CompoundGenerationMatrix
    {
        private const string ResourcePath = "Chemistry/compound-generation-matrix";
        private const string OxideIonId = "oxide";
        private static readonly object Sync = new object();
        private static Dictionary<string, ChemistryMatrixElement> elementsBySymbol;
        private static Dictionary<string, ChemistryIonDefinition> ionsById;
        private static Dictionary<string, MatrixCompoundOverrideRecord> overridesByCoordinate;
        private static Dictionary<string, string> exclusionsByCoordinate;
        private static List<ChemistryMatrixElement> elements;
        private static List<ChemistryIonDefinition> ions;
        private static List<GeneratedCompoundDefinition> generated;
        private static Dictionary<string, GeneratedCompoundDefinition> generatedByFormula;
        private static string schemaVersion;

        public static string SchemaVersion
        {
            get
            {
                EnsureLoaded();
                return schemaVersion;
            }
        }

        public static IReadOnlyList<ChemistryMatrixElement> Elements
        {
            get
            {
                EnsureLoaded();
                return elements;
            }
        }

        public static IReadOnlyList<ChemistryIonDefinition> Ions
        {
            get
            {
                EnsureLoaded();
                return ions;
            }
        }

        public static IReadOnlyList<GeneratedCompoundDefinition> GeneratedCompounds
        {
            get
            {
                EnsureLoaded();
                EnsureGenerated();
                return generated;
            }
        }

        public static int AcceptedCompoundCount
        {
            get { return GeneratedCompounds.Count; }
        }

        public static int UniqueFormulaCount
        {
            get
            {
                EnsureLoaded();
                EnsureGenerated();
                return generatedByFormula.Count;
            }
        }

        public static int ReviewedCompoundCount
        {
            get
            {
                var count = 0;
                var compounds = GeneratedCompounds;
                for (var index = 0; index < compounds.Count; index++)
                {
                    if (compounds[index].Confidence == CompoundConfidence.Reviewed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public static bool TryGetIon(string id, out ChemistryIonDefinition ion)
        {
            EnsureLoaded();
            ion = null;
            return id != null && ionsById.TryGetValue(id, out ion);
        }

        public static bool TryFindByFormula(
            string formula,
            out GeneratedCompoundDefinition compound)
        {
            EnsureLoaded();
            EnsureGenerated();
            compound = null;
            return formula != null && generatedByFormula.TryGetValue(formula, out compound);
        }

        public static bool TryGenerateIonicCompound(
            string cationId,
            string anionId,
            out GeneratedCompoundDefinition compound)
        {
            EnsureLoaded();
            compound = null;

            ChemistryIonDefinition cation;
            ChemistryIonDefinition anion;
            if (!ionsById.TryGetValue(cationId ?? string.Empty, out cation)
                || !ionsById.TryGetValue(anionId ?? string.Empty, out anion)
                || !cation.IsCation
                || !anion.IsAnion)
            {
                return false;
            }

            var coordinate = Coordinate(cation.Id, anion.Id);
            string rejection;
            if (exclusionsByCoordinate.TryGetValue(coordinate, out rejection))
            {
                return false;
            }

            var divisor = GreatestCommonDivisor(Math.Abs(cation.Charge), Math.Abs(anion.Charge));
            var cationCount = Math.Abs(anion.Charge) / divisor;
            var anionCount = Math.Abs(cation.Charge) / divisor;
            var family = ClassifyFamily(cation, anion);
            var solubility = EstimateSolubility(cation, anion);
            var phase = EstimatePhase(cation, anion, family, solubility);
            var formula = FormatIon(cation, cationCount) + FormatIon(anion, anionCount);
            var colour = DominantColour(cation, anion);
            var appearance = DescribeAppearance(colour, phase, solubility);
            var hazards = EstimateHazards(cation, anion, family, solubility);
            var confidence = CompoundConfidence.RuleDerived;
            var notes = "Charge-balanced and classified by high-school ion, solubility and hazard rules.";

            MatrixCompoundOverrideRecord reviewed;
            if (overridesByCoordinate.TryGetValue(coordinate, out reviewed))
            {
                formula = ValueOr(reviewed.formula, formula);
                solubility = ParseSolubility(reviewed.solubility, solubility);
                phase = ParsePhase(reviewed.phase, phase);
                colour = ValueOr(reviewed.colour, colour);
                appearance = ValueOr(reviewed.appearance, DescribeAppearance(colour, phase, solubility));
                hazards |= ParseHazards(reviewed.hazards);
                confidence = ParseConfidence(reviewed.confidence);
                notes = ValueOr(reviewed.notes, "Reviewed compound override.");
            }

            compound = new GeneratedCompoundDefinition(
                coordinate,
                BuildName(cation, anion, family),
                formula,
                cation.Id,
                anion.Id,
                cationCount,
                anionCount,
                cation.MolarMass * cationCount + anion.MolarMass * anionCount,
                anion.OxygenCount * anionCount,
                family,
                solubility,
                phase,
                appearance,
                colour,
                hazards,
                confidence,
                notes);
            return compound.IsAccepted;
        }

        public static bool TryGenerateOxide(
            string elementSymbol,
            int oxidationState,
            out GeneratedCompoundDefinition compound)
        {
            EnsureLoaded();
            compound = null;
            ChemistryMatrixElement element;
            ChemistryIonDefinition oxide;
            if (!elementsBySymbol.TryGetValue(elementSymbol ?? string.Empty, out element)
                || !ionsById.TryGetValue(OxideIonId, out oxide)
                || oxidationState <= 0
                || !Contains(element.OxidationStates, oxidationState))
            {
                return false;
            }

            var coordinate = "oxide:" + element.Symbol + ":" + oxidationState;
            string rejection;
            if (exclusionsByCoordinate.TryGetValue(coordinate, out rejection))
            {
                return false;
            }

            var divisor = GreatestCommonDivisor(oxidationState, Math.Abs(oxide.Charge));
            var elementCount = Math.Abs(oxide.Charge) / divisor;
            var oxygenCount = oxidationState / divisor;
            var formula = FormatElement(element.Symbol, elementCount)
                + FormatElement("O", oxygenCount);
            var family = element.Axis == ChemistryMatrixAxis.Metal
                ? GeneratedCompoundFamily.MetalOxide
                : GeneratedCompoundFamily.NonmetalOxide;
            var colour = element.Axis == ChemistryMatrixAxis.Metal ? "#E3E2DB" : "#DDE7E8";
            var solubility = element.Axis == ChemistryMatrixAxis.Metal
                ? CompoundSolubility.Insoluble
                : CompoundSolubility.ReactsWithWater;
            var phase = ChemicalPhase.Solid;
            var hazards = ChemicalHazardFlags.None;
            var confidence = CompoundConfidence.RuleDerived;
            var notes = "Empirical oxide formula derived from an allowed oxidation state.";

            MatrixCompoundOverrideRecord reviewed;
            if (overridesByCoordinate.TryGetValue(coordinate, out reviewed))
            {
                formula = ValueOr(reviewed.formula, formula);
                solubility = ParseSolubility(reviewed.solubility, solubility);
                phase = ParsePhase(reviewed.phase, phase);
                colour = ValueOr(reviewed.colour, colour);
                hazards |= ParseHazards(reviewed.hazards);
                confidence = ParseConfidence(reviewed.confidence);
                notes = ValueOr(reviewed.notes, notes);
            }

            compound = new GeneratedCompoundDefinition(
                coordinate,
                element.Name + " oxide (" + oxidationState + ")",
                formula,
                element.Symbol + "+" + oxidationState,
                OxideIonId,
                elementCount,
                oxygenCount,
                element.AtomicMass * elementCount + 15.999d * oxygenCount,
                oxygenCount,
                family,
                solubility,
                phase,
                DescribeAppearance(colour, phase, solubility),
                colour,
                hazards,
                confidence,
                notes);
            return compound.IsAccepted;
        }

        public static void ValidateOrThrow()
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(schemaVersion)
                || elements.Count < 20
                || ions.Count < 40)
            {
                throw new InvalidOperationException(
                    "Compound matrix data is incomplete. elements=" + elements.Count
                    + " ions=" + ions.Count + ".");
            }

            for (var index = 0; index < elements.Count; index++)
            {
                var element = elements[index];
                if (string.IsNullOrWhiteSpace(element.Symbol)
                    || element.AtomicMass <= 0d
                    || element.OxidationStates.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Invalid compound-matrix element at index " + index + ".");
                }

                var periodic = FindPeriodicElement(element.Symbol);
                if (periodic == null
                    || Math.Abs(periodic.AtomicMass - element.AtomicMass) > .15d)
                {
                    throw new InvalidOperationException(
                        "Compound-matrix element is not aligned with the periodic table: "
                        + element.Symbol + ".");
                }
            }

            for (var index = 0; index < ions.Count; index++)
            {
                var ion = ions[index];
                if (ion.Charge == 0
                    || ion.MolarMass <= 0d
                    || string.IsNullOrWhiteSpace(ion.Formula))
                {
                    throw new InvalidOperationException(
                        "Invalid compound-matrix ion at index " + index + ".");
                }
            }

            AssertCompound("sodium", "sulfate", "Na₂SO₄", CompoundSolubility.Soluble);
            AssertCompound("calcium", "phosphate", "Ca₃(PO₄)₂", CompoundSolubility.Insoluble);
            AssertCompound("copper-two", "hydroxide", "Cu(OH)₂", CompoundSolubility.Insoluble);
            AssertCompound("hydrogen", "acetate", "CH₃COOH", CompoundSolubility.Soluble);
            AssertOxide("Al", 3, "Al₂O₃");
            AssertOxide("S", 6, "SO₃");

            if (AcceptedCompoundCount < 450 || ReviewedCompoundCount < 20)
            {
                throw new InvalidOperationException(
                    "Compound generation coverage is unexpectedly low. accepted="
                    + AcceptedCompoundCount + " reviewed=" + ReviewedCompoundCount + ".");
            }
        }

        internal static ChemistryMatrixAxis ParseAxis(string value)
        {
            ChemistryMatrixAxis parsed;
            return Enum.TryParse(value, true, out parsed)
                ? parsed
                : ChemistryMatrixAxis.PolyatomicIon;
        }

        internal static ChemicalHazardFlags ParseHazards(string[] values)
        {
            var result = ChemicalHazardFlags.None;
            if (values == null)
            {
                return result;
            }

            for (var index = 0; index < values.Length; index++)
            {
                ChemicalHazardFlags parsed;
                var normalized = (values[index] ?? string.Empty).Replace("-", string.Empty);
                if (Enum.TryParse(normalized, true, out parsed))
                {
                    result |= parsed;
                }
            }

            return result;
        }

        private static void EnsureLoaded()
        {
            if (ionsById != null)
            {
                return;
            }

            lock (Sync)
            {
                if (ionsById != null)
                {
                    return;
                }

                var asset = Resources.Load<TextAsset>(ResourcePath);
                if (asset == null)
                {
                    throw new InvalidOperationException(
                        "Missing Resources/" + ResourcePath + ".json.");
                }

                var document = JsonUtility.FromJson<CompoundMatrixDocument>(asset.text);
                if (document == null || document.elements == null || document.ions == null)
                {
                    throw new InvalidOperationException("Compound matrix JSON could not be parsed.");
                }

                schemaVersion = document.schemaVersion;
                elements = new List<ChemistryMatrixElement>(document.elements.Length);
                elementsBySymbol = new Dictionary<string, ChemistryMatrixElement>(StringComparer.Ordinal);
                for (var index = 0; index < document.elements.Length; index++)
                {
                    var definition = new ChemistryMatrixElement(document.elements[index]);
                    elements.Add(definition);
                    elementsBySymbol.Add(definition.Symbol, definition);
                }

                ions = new List<ChemistryIonDefinition>(document.ions.Length);
                ionsById = new Dictionary<string, ChemistryIonDefinition>(StringComparer.Ordinal);
                for (var index = 0; index < document.ions.Length; index++)
                {
                    var definition = new ChemistryIonDefinition(document.ions[index]);
                    ions.Add(definition);
                    ionsById.Add(definition.Id, definition);
                }

                overridesByCoordinate =
                    new Dictionary<string, MatrixCompoundOverrideRecord>(StringComparer.Ordinal);
                var sourceOverrides = document.overrides ?? new MatrixCompoundOverrideRecord[0];
                for (var index = 0; index < sourceOverrides.Length; index++)
                {
                    overridesByCoordinate.Add(sourceOverrides[index].coordinate, sourceOverrides[index]);
                }

                exclusionsByCoordinate = new Dictionary<string, string>(StringComparer.Ordinal);
                var sourceExclusions = document.exclusions ?? new MatrixExclusionRecord[0];
                for (var index = 0; index < sourceExclusions.Length; index++)
                {
                    exclusionsByCoordinate.Add(
                        sourceExclusions[index].coordinate,
                        sourceExclusions[index].reason);
                }
            }
        }

        private static void EnsureGenerated()
        {
            if (generated != null)
            {
                return;
            }

            lock (Sync)
            {
                if (generated != null)
                {
                    return;
                }

                var all = new List<GeneratedCompoundDefinition>();
                var formulas = new Dictionary<string, GeneratedCompoundDefinition>(StringComparer.Ordinal);
                for (var cationIndex = 0; cationIndex < ions.Count; cationIndex++)
                {
                    var cation = ions[cationIndex];
                    if (!cation.IsCation)
                    {
                        continue;
                    }

                    for (var anionIndex = 0; anionIndex < ions.Count; anionIndex++)
                    {
                        var anion = ions[anionIndex];
                        GeneratedCompoundDefinition compound;
                        if (!anion.IsAnion
                            || !TryGenerateIonicCompound(cation.Id, anion.Id, out compound))
                        {
                            continue;
                        }

                        all.Add(compound);
                        AddPreferredFormula(formulas, compound);
                    }
                }

                for (var elementIndex = 0; elementIndex < elements.Count; elementIndex++)
                {
                    var element = elements[elementIndex];
                    for (var stateIndex = 0; stateIndex < element.OxidationStates.Count; stateIndex++)
                    {
                        var state = element.OxidationStates[stateIndex];
                        GeneratedCompoundDefinition oxide;
                        if (state > 0 && TryGenerateOxide(element.Symbol, state, out oxide))
                        {
                            all.Add(oxide);
                            AddPreferredFormula(formulas, oxide);
                        }
                    }
                }

                generated = all;
                generatedByFormula = formulas;
            }
        }

        private static void AddPreferredFormula(
            IDictionary<string, GeneratedCompoundDefinition> index,
            GeneratedCompoundDefinition candidate)
        {
            GeneratedCompoundDefinition current;
            if (!index.TryGetValue(candidate.Formula, out current)
                || candidate.Confidence < current.Confidence)
            {
                index[candidate.Formula] = candidate;
            }
        }

        private static GeneratedCompoundFamily ClassifyFamily(
            ChemistryIonDefinition cation,
            ChemistryIonDefinition anion)
        {
            if (cation.Id == "hydrogen")
            {
                return anion.Id == OxideIonId || anion.Id == "hydroxide"
                    ? GeneratedCompoundFamily.Molecular
                    : GeneratedCompoundFamily.Acid;
            }

            if (cation.Id == "ammonium")
            {
                return GeneratedCompoundFamily.AmmoniumSalt;
            }

            if (anion.Id == OxideIonId)
            {
                return GeneratedCompoundFamily.MetalOxide;
            }

            if (anion.Id == "hydroxide")
            {
                return GeneratedCompoundFamily.Hydroxide;
            }

            return anion.OxygenCount > 0
                ? GeneratedCompoundFamily.OxySalt
                : GeneratedCompoundFamily.BinarySalt;
        }

        private static CompoundSolubility EstimateSolubility(
            ChemistryIonDefinition cation,
            ChemistryIonDefinition anion)
        {
            if (cation.Id == "hydrogen")
            {
                return CompoundSolubility.Soluble;
            }

            if (cation.Id == "lithium"
                || cation.Id == "sodium"
                || cation.Id == "potassium"
                || cation.Id == "ammonium")
            {
                return anion.Id == OxideIonId
                    ? CompoundSolubility.ReactsWithWater
                    : CompoundSolubility.Soluble;
            }

            if (anion.Id == "nitrate"
                || anion.Id == "nitrite"
                || anion.Id == "acetate"
                || anion.Id == "chlorate"
                || anion.Id == "hypochlorite"
                || anion.Id == "bicarbonate"
                || anion.Id == "dichromate")
            {
                return CompoundSolubility.Soluble;
            }

            if (anion.Id == "chloride" || anion.Id == "bromide" || anion.Id == "iodide")
            {
                return cation.Id == "silver" || cation.Id.StartsWith("lead-", StringComparison.Ordinal)
                    ? CompoundSolubility.Insoluble
                    : CompoundSolubility.Soluble;
            }

            if (anion.Id == "sulfate")
            {
                if (cation.Id == "barium" || cation.Id.StartsWith("lead-", StringComparison.Ordinal))
                {
                    return CompoundSolubility.Insoluble;
                }

                return cation.Id == "calcium"
                    ? CompoundSolubility.SlightlySoluble
                    : CompoundSolubility.Soluble;
            }

            if (anion.Id == "hydroxide")
            {
                if (cation.Id == "barium")
                {
                    return CompoundSolubility.Soluble;
                }

                return cation.Id == "calcium"
                    ? CompoundSolubility.SlightlySoluble
                    : CompoundSolubility.Insoluble;
            }

            if (anion.Id == OxideIonId)
            {
                return cation.Id == "calcium" || cation.Id == "barium"
                    ? CompoundSolubility.ReactsWithWater
                    : CompoundSolubility.Insoluble;
            }

            if (anion.Id == "carbonate"
                || anion.Id == "phosphate"
                || anion.Id == "sulfide"
                || anion.Id == "silicate"
                || anion.Id == "chromate")
            {
                return CompoundSolubility.Insoluble;
            }

            return CompoundSolubility.Unknown;
        }

        private static ChemicalPhase EstimatePhase(
            ChemistryIonDefinition cation,
            ChemistryIonDefinition anion,
            GeneratedCompoundFamily family,
            CompoundSolubility solubility)
        {
            if (cation.Id == "hydrogen" && anion.Id == "sulfide")
            {
                return ChemicalPhase.Gas;
            }

            if (cation.Id == "hydrogen")
            {
                return ChemicalPhase.Liquid;
            }

            return ChemicalPhase.Solid;
        }

        private static ChemicalHazardFlags EstimateHazards(
            ChemistryIonDefinition cation,
            ChemistryIonDefinition anion,
            GeneratedCompoundFamily family,
            CompoundSolubility solubility)
        {
            var hazards = cation.Hazards | anion.Hazards;
            if (family == GeneratedCompoundFamily.Acid
                || family == GeneratedCompoundFamily.Hydroxide
                && solubility == CompoundSolubility.Soluble)
            {
                hazards |= ChemicalHazardFlags.Corrosive;
            }

            if (family == GeneratedCompoundFamily.Acid
                && (anion.Id == "carbonate"
                    || anion.Id == "sulfide"
                    || anion.Id == "nitrite"
                    || anion.Id == "hypochlorite"))
            {
                hazards |= ChemicalHazardFlags.GasReleasePotential;
            }

            return hazards;
        }

        private static string DominantColour(
            ChemistryIonDefinition cation,
            ChemistryIonDefinition anion)
        {
            if (!string.Equals(anion.Colour, "#ECECE8", StringComparison.OrdinalIgnoreCase))
            {
                return anion.Colour;
            }

            return cation.Colour;
        }

        private static string DescribeAppearance(
            string colour,
            ChemicalPhase phase,
            CompoundSolubility solubility)
        {
            if (phase == ChemicalPhase.Gas)
            {
                return "Generated gas; colour is an estimated display cue.";
            }

            if (phase == ChemicalPhase.Liquid || phase == ChemicalPhase.Aqueous)
            {
                return "Generated liquid/solution with estimated colour " + colour + ".";
            }

            return solubility == CompoundSolubility.Insoluble
                || solubility == CompoundSolubility.SlightlySoluble
                ? "Solid; may appear as a precipitate. Estimated colour " + colour + "."
                : "Ionic solid with estimated colour " + colour + ".";
        }

        private static string BuildName(
            ChemistryIonDefinition cation,
            ChemistryIonDefinition anion,
            GeneratedCompoundFamily family)
        {
            if (family == GeneratedCompoundFamily.Acid)
            {
                return anion.Name + " acid";
            }

            if (family == GeneratedCompoundFamily.Molecular)
            {
                return "Water-derived molecular compound";
            }

            return cation.Name + " " + anion.Name;
        }

        private static string FormatIon(ChemistryIonDefinition ion, int count)
        {
            if (count <= 1)
            {
                return ion.Formula;
            }

            return (ion.IsPolyatomic ? "(" + ion.Formula + ")" : ion.Formula) + Subscript(count);
        }

        private static string FormatElement(string symbol, int count)
        {
            return count <= 1 ? symbol : symbol + Subscript(count);
        }

        private static string Subscript(int value)
        {
            const string digits = "₀₁₂₃₄₅₆₇₈₉";
            var source = value.ToString();
            var result = new StringBuilder(source.Length);
            for (var index = 0; index < source.Length; index++)
            {
                result.Append(digits[source[index] - '0']);
            }

            return result.ToString();
        }

        private static string Coordinate(string cationId, string anionId)
        {
            return cationId + "|" + anionId;
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
            {
                var next = a % b;
                a = b;
                b = next;
            }

            return Math.Abs(a);
        }

        private static bool Contains(IReadOnlyList<int> values, int expected)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (values[index] == expected)
                {
                    return true;
                }
            }

            return false;
        }

        private static PeriodicElementDefinition FindPeriodicElement(string symbol)
        {
            var all = HighSchoolPeriodicTable.All;
            for (var index = 0; index < all.Count; index++)
            {
                if (string.Equals(all[index].Symbol, symbol, StringComparison.Ordinal))
                {
                    return all[index];
                }
            }

            return null;
        }

        private static string ValueOr(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static CompoundSolubility ParseSolubility(
            string value,
            CompoundSolubility fallback)
        {
            CompoundSolubility parsed;
            return Enum.TryParse(value, true, out parsed) ? parsed : fallback;
        }

        private static ChemicalPhase ParsePhase(string value, ChemicalPhase fallback)
        {
            ChemicalPhase parsed;
            return Enum.TryParse(value, true, out parsed) ? parsed : fallback;
        }

        private static CompoundConfidence ParseConfidence(string value)
        {
            CompoundConfidence parsed;
            return Enum.TryParse(value, true, out parsed)
                ? parsed
                : CompoundConfidence.Reviewed;
        }

        private static void AssertCompound(
            string cationId,
            string anionId,
            string formula,
            CompoundSolubility solubility)
        {
            GeneratedCompoundDefinition compound;
            if (!TryGenerateIonicCompound(cationId, anionId, out compound)
                || compound.Formula != formula
                || compound.Solubility != solubility
                || Math.Abs(
                    compound.CationCount * GetIonCharge(cationId)
                    + compound.AnionCount * GetIonCharge(anionId)) != 0)
            {
                throw new InvalidOperationException(
                    "Compound matrix assertion failed for " + cationId + " + " + anionId + ".");
            }
        }

        private static void AssertOxide(string symbol, int state, string formula)
        {
            GeneratedCompoundDefinition compound;
            if (!TryGenerateOxide(symbol, state, out compound) || compound.Formula != formula)
            {
                throw new InvalidOperationException(
                    "Oxide matrix assertion failed for " + symbol + "(" + state + ").");
            }
        }

        private static int GetIonCharge(string id)
        {
            ChemistryIonDefinition ion;
            return ionsById.TryGetValue(id, out ion) ? ion.Charge : 0;
        }
    }

}
