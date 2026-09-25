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
        public Matrix2DRecord matrix2D;
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
        public string propertyReviewStatus;
    }

    [Serializable]
    internal sealed class MatrixExclusionRecord
    {
        public string coordinate;
        public string reason;
    }

    [Serializable]
    internal sealed class Matrix2DRecord
    {
        public string rowAxis;
        public string columnAxis;
        public string defaultStatus;
        public string[] defaultConditionIds;
        public string[] defaultEvidenceIds;
        public MatrixCellRecord[] cells;
        public MatrixConditionRecord[] conditions;
        public MatrixEvidenceRecord[] evidence;
    }

    [Serializable]
    internal sealed class MatrixCellRecord
    {
        public string cationId;
        public string anionId;
        public string status;
        public string[] conditionIds;
        public string[] evidenceIds;
        public string notes;
    }

    [Serializable]
    internal sealed class MatrixConditionRecord
    {
        public string id;
        public string label;
        public string description;
    }

    [Serializable]
    internal sealed class MatrixEvidenceRecord
    {
        public string id;
        public string title;
        public string url;
        public string scope;
        public string accessedOn;
    }
}
