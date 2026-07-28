using System;

namespace ChemistryLab.Desktop
{
    [Serializable]
    internal sealed class CompoundMatrixDocument
    {
        public string schemaVersion;
        public MatrixElementRecord[] elements;
        public MatrixIonRecord[] ions;
        public MatrixCompoundOverrideRecord[] overrides;
        public MatrixExclusionRecord[] exclusions;
    }

    [Serializable]
    internal sealed class MatrixElementRecord
    {
        public string symbol;
        public string name;
        public double atomicMass;
        public string axis;
        public int activityRank;
        public int[] oxidationStates;
    }

    [Serializable]
    internal sealed class MatrixIonRecord
    {
        public string id;
        public string name;
        public string formula;
        public int charge;
        public double molarMass;
        public string elementSymbol;
        public int oxygenCount;
        public bool polyatomic;
        public string colour;
        public string[] hazards;
    }

    [Serializable]
    internal sealed class MatrixCompoundOverrideRecord
    {
        public string coordinate;
        public string formula;
        public string solubility;
        public string phase;
        public string appearance;
        public string colour;
        public string[] hazards;
        public string confidence;
        public string notes;
    }

    [Serializable]
    internal sealed class MatrixExclusionRecord
    {
        public string coordinate;
        public string reason;
    }
}
