using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    public enum ChemistryMatrixAxis
    {
        Metal,
        Nonmetal,
        Oxygen,
        PolyatomicIon
    }

    public enum GeneratedCompoundFamily
    {
        BinarySalt,
        OxySalt,
        Acid,
        Hydroxide,
        MetalOxide,
        NonmetalOxide,
        AmmoniumSalt,
        Molecular
    }

    public enum CompoundSolubility
    {
        Unknown,
        Soluble,
        SlightlySoluble,
        Insoluble,
        ReactsWithWater
    }

    public enum CompoundConfidence
    {
        Reviewed,
        RuleDerived,
        Rejected
    }

    [Flags]
    public enum ChemicalHazardFlags
    {
        None = 0,
        Corrosive = 1 << 0,
        Toxic = 1 << 1,
        Oxidizer = 1 << 2,
        EnvironmentalHazard = 1 << 3,
        WaterReactive = 1 << 4,
        GasReleasePotential = 1 << 5,
        HeavyMetal = 1 << 6,
        Carcinogenic = 1 << 7
    }

    /// <summary>
    /// One element on the proposed X/Y/Z chemistry space. The oxidation states are
    /// explicit because a position on a simple metal/nonmetal axis is not enough to
    /// determine a chemically meaningful formula.
    /// </summary>
    public sealed class ChemistryMatrixElement
    {
        internal ChemistryMatrixElement(MatrixElementRecord source)
        {
            Symbol = source.symbol;
            Name = source.name;
            AtomicMass = source.atomicMass;
            Axis = CompoundGenerationMatrix.ParseAxis(source.axis);
            ActivityRank = source.activityRank;
            OxidationStates = Array.AsReadOnly(source.oxidationStates ?? new int[0]);
        }

        public string Symbol { get; private set; }
        public string Name { get; private set; }
        public double AtomicMass { get; private set; }
        public ChemistryMatrixAxis Axis { get; private set; }
        public int ActivityRank { get; private set; }
        public IReadOnlyList<int> OxidationStates { get; private set; }
    }

    /// <summary>
    /// A reusable ion node. Positive and negative charges keep their mathematical
    /// sign; formula generation therefore reduces a real charge ratio.
    /// </summary>
    public sealed class ChemistryIonDefinition
    {
        internal ChemistryIonDefinition(MatrixIonRecord source)
        {
            Id = source.id;
            Name = source.name;
            Formula = source.formula;
            Charge = source.charge;
            MolarMass = source.molarMass;
            ElementSymbol = source.elementSymbol;
            OxygenCount = source.oxygenCount;
            IsPolyatomic = source.polyatomic;
            Colour = string.IsNullOrWhiteSpace(source.colour) ? "#ECECE8" : source.colour;
            Hazards = CompoundGenerationMatrix.ParseHazards(source.hazards);
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Formula { get; private set; }
        public int Charge { get; private set; }
        public double MolarMass { get; private set; }
        public string ElementSymbol { get; private set; }
        public int OxygenCount { get; private set; }
        public bool IsPolyatomic { get; private set; }
        public string Colour { get; private set; }
        public ChemicalHazardFlags Hazards { get; private set; }
        public bool IsCation { get { return Charge > 0; } }
        public bool IsAnion { get { return Charge < 0; } }
    }

    /// <summary>
    /// Immutable output of the compound generator. Physical properties are
    /// classified estimates, never invented precision. Reviewed overrides take
    /// priority over general high-school rules.
    /// </summary>
    public sealed class GeneratedCompoundDefinition
    {
        internal GeneratedCompoundDefinition(
            string coordinate,
            string name,
            string formula,
            string cationId,
            string anionId,
            int cationCount,
            int anionCount,
            double molarMass,
            int oxygenCount,
            GeneratedCompoundFamily family,
            CompoundSolubility solubility,
            ChemicalPhase phase,
            string appearance,
            string colour,
            ChemicalHazardFlags hazards,
            CompoundConfidence confidence,
            string validationNotes)
        {
            Coordinate = coordinate;
            Name = name;
            Formula = formula;
            CationId = cationId;
            AnionId = anionId;
            CationCount = cationCount;
            AnionCount = anionCount;
            MolarMass = molarMass;
            OxygenCount = oxygenCount;
            Family = family;
            Solubility = solubility;
            Phase = phase;
            Appearance = appearance;
            Colour = colour;
            Hazards = hazards;
            Confidence = confidence;
            ValidationNotes = validationNotes;
        }

        public string Coordinate { get; private set; }
        public string Name { get; private set; }
        public string Formula { get; private set; }
        public string CationId { get; private set; }
        public string AnionId { get; private set; }
        public int CationCount { get; private set; }
        public int AnionCount { get; private set; }
        public double MolarMass { get; private set; }
        public int OxygenCount { get; private set; }
        public GeneratedCompoundFamily Family { get; private set; }
        public CompoundSolubility Solubility { get; private set; }
        public ChemicalPhase Phase { get; private set; }
        public string Appearance { get; private set; }
        public string Colour { get; private set; }
        public ChemicalHazardFlags Hazards { get; private set; }
        public CompoundConfidence Confidence { get; private set; }
        public string ValidationNotes { get; private set; }
        public bool IsAccepted { get { return Confidence != CompoundConfidence.Rejected; } }
        public float ConfidenceScore
        {
            get
            {
                switch (Confidence)
                {
                    case CompoundConfidence.Reviewed: return .98f;
                    case CompoundConfidence.RuleDerived: return .72f;
                    default: return 0f;
                }
            }
        }
    }
}
