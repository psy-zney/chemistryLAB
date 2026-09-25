using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace ChemistryLab.Desktop
{
    /// <summary>A formal composition coordinate. Neither its status nor its ratio authorizes a reaction.</summary>
    public sealed class CompoundMatrixCell
    {
        internal CompoundMatrixCell(string anionId, string cationId, string coordinate,
            string formula, int cationCount, int anionCount, string status,
            string[] conditionIds, string[] evidenceIds, string notes, string exceptionReason,
            string propertyReviewStatus)
        {
            AnionId = anionId;
            CationId = cationId;
            Coordinate = coordinate;
            Formula = formula;
            FormulaAscii = ChemicalFormulaComposition.ToAscii(formula);
            CationCount = cationCount;
            AnionCount = anionCount;
            Status = status;
            ConditionIds = Array.AsReadOnly((string[])conditionIds.Clone());
            EvidenceIds = Array.AsReadOnly((string[])evidenceIds.Clone());
            Notes = notes;
            ExceptionReason = exceptionReason;
            PropertyReviewStatus = propertyReviewStatus;
        }

        public string AnionId { get; private set; }
        public string CationId { get; private set; }
        public string Coordinate { get; private set; }
        public string Formula { get; private set; }
        public string FormulaAscii { get; private set; }
        public int CationCount { get; private set; }
        public int AnionCount { get; private set; }
        public string Status { get; private set; }
        public IReadOnlyList<string> ConditionIds { get; private set; }
        public IReadOnlyList<string> EvidenceIds { get; private set; }
        public string Notes { get; private set; }
        public string ExceptionReason { get; private set; }
        public string PropertyReviewStatus { get; private set; }
        public bool AuthorizesReaction { get { return false; } }
    }

    /// <summary>Descriptive scope for the cited claim, not an executable condition predicate.</summary>
    public sealed class CompoundMatrixCondition
    {
        internal CompoundMatrixCondition(MatrixConditionRecord source)
        { Id = source.id; Label = source.label; Description = source.description; }
        public string Id { get; private set; }
        public string Label { get; private set; }
        public string Description { get; private set; }
        public bool IsExecutablePredicate { get { return false; } }
    }

    public sealed class CompoundMatrixEvidence
    {
        internal CompoundMatrixEvidence(MatrixEvidenceRecord source)
        { Id = source.id; Title = source.title; Url = source.url; Scope = source.scope; AccessedOn = source.accessedOn; }
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Url { get; private set; }
        public string Scope { get; private set; }
        public string AccessedOn { get; private set; }
    }

    public static partial class CompoundGenerationMatrix
    {
        private const string FormalNotes = "Formal charge-balanced composition; existence, stability and reaction feasibility have not been established for this cell.";
        private static Matrix2DRecord matrix2D;
        private static Dictionary<string, MatrixCellRecord> cellAnnotations;
        private static IReadOnlyList<CompoundMatrixCell> matrixCells;
        private static IReadOnlyList<CompoundMatrixCondition> matrixConditions;
        private static IReadOnlyList<CompoundMatrixEvidence> matrixEvidence;

        public static IReadOnlyList<CompoundMatrixCell> Cells
        {
            get
            {
                EnsureLoaded();
                if (matrixCells == null)
                {
                    var result = new List<CompoundMatrixCell>();
                    foreach (var anion in ions)
                    {
                        if (!anion.IsAnion) continue;
                        foreach (var cation in ions)
                        {
                            CompoundMatrixCell cell;
                            if (cation.IsCation && TryGetCell(anion.Id, cation.Id, out cell)) result.Add(cell);
                        }
                    }
                    matrixCells = result.AsReadOnly();
                }
                return matrixCells;
            }
        }

        public static IReadOnlyList<CompoundMatrixCondition> Conditions { get { EnsureLoaded(); return matrixConditions; } }
        public static IReadOnlyList<CompoundMatrixEvidence> Evidence { get { EnsureLoaded(); return matrixEvidence; } }

        // Explicit conversion avoids accidentally swapping the row/column order in legacy consumers.
        public static string ToLegacyCoordinate(string cationId, string anionId)
        { return Coordinate(cationId, anionId); }

        public static bool TryGetCell(string anionId, string cationId, out CompoundMatrixCell cell)
        {
            EnsureLoaded();
            cell = null;
            ChemistryIonDefinition cation, anion;
            if (!ionsById.TryGetValue(cationId ?? string.Empty, out cation) || !cation.IsCation
                || !ionsById.TryGetValue(anionId ?? string.Empty, out anion) || !anion.IsAnion) return false;
            var coordinate = ToLegacyCoordinate(cationId, anionId);
            var divisor = GreatestCommonDivisor(cation.Charge, -anion.Charge);
            var cationCount = -anion.Charge / divisor;
            var anionCount = cation.Charge / divisor;
            MatrixCellRecord annotation;
            MatrixCompoundOverrideRecord property;
            string exclusion;
            cellAnnotations.TryGetValue(coordinate, out annotation);
            overridesByCoordinate.TryGetValue(coordinate, out property);
            exclusionsByCoordinate.TryGetValue(coordinate, out exclusion);
            var formula = FormatIon(cation, cationCount) + FormatIon(anion, anionCount);
            cell = new CompoundMatrixCell(anionId, cationId, coordinate,
                exclusion != null ? null : property != null ? ValueOr(property.formula, formula) : formula,
                cationCount, anionCount, exclusion != null ? "excluded" : annotation != null ? annotation.status : matrix2D.defaultStatus,
                annotation != null && annotation.conditionIds != null ? annotation.conditionIds : matrix2D.defaultConditionIds,
                annotation != null && annotation.evidenceIds != null ? annotation.evidenceIds : matrix2D.defaultEvidenceIds,
                annotation != null && annotation.notes != null ? annotation.notes : FormalNotes,
                exclusion, property != null ? property.propertyReviewStatus : "heuristic");
            return true;
        }

        private static void InitializeMatrix2D(Matrix2DRecord source)
        {
            if (source == null || source.rowAxis != "anionId" || source.columnAxis != "cationId"
                || source.defaultStatus != "formalComposition" || source.cells == null
                || source.conditions == null || source.evidence == null
                || source.defaultConditionIds == null || source.defaultEvidenceIds == null)
                throw new InvalidOperationException("Missing or invalid canonical anion/cation matrix contract.");
            matrix2D = source;
            cellAnnotations = new Dictionary<string, MatrixCellRecord>(StringComparer.Ordinal);
            foreach (var cell in source.cells)
            {
                if (cell == null) throw new InvalidOperationException("Null matrix annotation.");
                cellAnnotations.Add(ToLegacyCoordinate(cell.cationId, cell.anionId), cell);
            }
            var conditions = new List<CompoundMatrixCondition>();
            foreach (var record in source.conditions)
            {
                if (record == null) throw new InvalidOperationException("Null matrix condition.");
                conditions.Add(new CompoundMatrixCondition(record));
            }
            matrixConditions = conditions.AsReadOnly();
            var evidence = new List<CompoundMatrixEvidence>();
            foreach (var record in source.evidence)
            {
                if (record == null) throw new InvalidOperationException("Null matrix evidence.");
                evidence.Add(new CompoundMatrixEvidence(record));
            }
            matrixEvidence = evidence.AsReadOnly();
        }

        private static void ValidateMatrix2DOrThrow()
        {
            var conditionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var condition in Conditions)
                if (string.IsNullOrWhiteSpace(condition.Id) || !conditionIds.Add(condition.Id)
                    || string.IsNullOrWhiteSpace(condition.Label) || string.IsNullOrWhiteSpace(condition.Description))
                    throw new InvalidOperationException("Invalid or duplicate condition: " + condition.Id);
            var evidenceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var evidence in Evidence)
            {
                Uri uri;
                DateTime date;
                if (string.IsNullOrWhiteSpace(evidence.Id) || !evidenceIds.Add(evidence.Id)
                    || string.IsNullOrWhiteSpace(evidence.Title) || string.IsNullOrWhiteSpace(evidence.Scope)
                    || !Uri.TryCreate(evidence.Url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps
                    || !DateTime.TryParseExact(evidence.AccessedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    throw new InvalidOperationException("Invalid or duplicate evidence: " + evidence.Id);
            }
            ValidateReferences(matrix2D.defaultConditionIds, conditionIds, "default conditions");
            ValidateReferences(matrix2D.defaultEvidenceIds, evidenceIds, "default evidence");
            foreach (var ion in ions)
            {
                int oxygen;
                ion.AtomCounts.TryGetValue("O", out oxygen);
                if (string.IsNullOrWhiteSpace(ion.Id) || ion.Id.Contains("|") || ion.Charge == 0 || Math.Abs((long)ion.Charge) > 16
                    || double.IsNaN(ion.MolarMass) || double.IsInfinity(ion.MolarMass) || ion.MolarMass <= 0d
                    || oxygen != ion.OxygenCount
                    || (!ion.IsPolyatomic && FindPeriodicElement(ion.ElementSymbol) == null))
                    throw new InvalidOperationException("Invalid ion identity/composition: " + ion.Id);
                foreach (var atom in ion.AtomCounts)
                    if (FindPeriodicElement(atom.Key) == null) throw new InvalidOperationException("Unknown element in " + ion.Id);
            }
            foreach (var annotation in cellAnnotations.Values)
            {
                ValidateCoordinate(annotation.cationId + "|" + annotation.anionId);
                if (annotation.status != "formalComposition" && annotation.status != "literatureSupported" && annotation.status != "unsupported")
                    throw new InvalidOperationException("Invalid annotation status: " + annotation.status);
                if (string.IsNullOrWhiteSpace(annotation.notes)) throw new InvalidOperationException("A cell annotation requires scoped notes.");
                ValidateReferences(annotation.conditionIds ?? matrix2D.defaultConditionIds, conditionIds, "cell conditions");
                ValidateReferences(annotation.evidenceIds ?? matrix2D.defaultEvidenceIds, evidenceIds, "cell evidence");
                if (annotation.status == "literatureSupported" && (annotation.evidenceIds == null || annotation.evidenceIds.Length == 0))
                    throw new InvalidOperationException("Literature-supported cells require explicit evidence.");
            }
            foreach (var exclusion in exclusionsByCoordinate)
            {
                ValidateCoordinate(exclusion.Key);
                if (string.IsNullOrWhiteSpace(exclusion.Value)) throw new InvalidOperationException("Missing exclusion reason: " + exclusion.Key);
            }
            foreach (var property in overridesByCoordinate.Values)
            {
                var expected = ValidateCoordinate(property.coordinate);
                if (property.propertyReviewStatus != "propertyReviewed" || property.confidence != "Reviewed")
                    throw new InvalidOperationException("Override must identify internal property review: " + property.coordinate);
                if (!string.IsNullOrWhiteSpace(property.formula))
                    ChemicalFormulaComposition.AssertEqual(expected, ChemicalFormulaComposition.Parse(property.formula), property.coordinate);
            }
            foreach (var cell in Cells)
            {
                var cation = ionsById[cell.CationId];
                var anion = ionsById[cell.AnionId];
                if ((long)cell.CationCount * cation.Charge + (long)cell.AnionCount * anion.Charge != 0
                    || cell.CationCount <= 0 || cell.AnionCount <= 0 || GreatestCommonDivisor(cell.CationCount, cell.AnionCount) != 1)
                    throw new InvalidOperationException("Non-neutral or non-reduced cell: " + cell.Coordinate);
                GeneratedCompoundDefinition legacy;
                if (cell.Status == "excluded")
                {
                    if (cell.Formula != null || TryGenerateIonicCompound(cell.CationId, cell.AnionId, out legacy))
                        throw new InvalidOperationException("Exclusion precedence failed: " + cell.Coordinate);
                    continue;
                }
                ChemicalFormulaComposition.AssertEqual(ValidateCoordinate(cell.Coordinate), ChemicalFormulaComposition.Parse(cell.Formula), cell.Coordinate);
                if (!TryGenerateIonicCompound(cell.CationId, cell.AnionId, out legacy) || legacy.Formula != cell.Formula
                    || legacy.CationCount != cell.CationCount || legacy.AnionCount != cell.AnionCount)
                    throw new InvalidOperationException("Legacy matrix parity failed: " + cell.Coordinate);
            }
        }

        private static void ValidateReferences(IEnumerable<string> references, HashSet<string> known, string context)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in references)
                if (id == null || !known.Contains(id) || !seen.Add(id)) throw new InvalidOperationException("Invalid reference in " + context + ": " + id);
        }

        private static IReadOnlyDictionary<string, int> ValidateCoordinate(string coordinate)
        {
            if (coordinate.StartsWith("oxide:", StringComparison.Ordinal))
            {
                var parts = coordinate.Split(':');
                ChemistryMatrixElement element;
                int state;
                if (parts.Length != 3 || !elementsBySymbol.TryGetValue(parts[1], out element)
                    || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out state)
                    || state <= 0 || !Contains(element.OxidationStates, state))
                    throw new InvalidOperationException("Invalid oxide coordinate: " + coordinate);
                var divisor = GreatestCommonDivisor(state, 2);
                return ChemicalFormulaComposition.Parse(FormatElement(element.Symbol, 2 / divisor) + FormatElement("O", state / divisor));
            }
            var pair = coordinate.Split('|');
            ChemistryIonDefinition cation, anion;
            if (pair.Length != 2 || !ionsById.TryGetValue(pair[0], out cation) || !cation.IsCation
                || !ionsById.TryGetValue(pair[1], out anion) || !anion.IsAnion)
                throw new InvalidOperationException("Invalid ionic coordinate: " + coordinate);
            var gcd = GreatestCommonDivisor(cation.Charge, -anion.Charge);
            var atoms = new Dictionary<string, int>(StringComparer.Ordinal);
            ChemicalFormulaComposition.Add(atoms, cation.AtomCounts, -anion.Charge / gcd);
            ChemicalFormulaComposition.Add(atoms, anion.AtomCounts, cation.Charge / gcd);
            return new ReadOnlyDictionary<string, int>(atoms);
        }
    }

    /// <summary>Strict parser for the shipped element/group formula subset; charges remain separate ion data.</summary>
    internal static class ChemicalFormulaComposition
    {
        internal static string ToAscii(string formula)
        {
            if (formula == null) return null;
            var result = new StringBuilder(formula.Length);
            foreach (var value in formula) result.Append(value >= '\u2080' && value <= '\u2089' ? (char)('0' + value - '\u2080') : value);
            return result.ToString();
        }

        internal static IReadOnlyDictionary<string, int> Parse(string formula)
        {
            var source = ToAscii(formula);
            if (string.IsNullOrEmpty(source)) throw new InvalidOperationException("Missing chemical formula.");
            var index = 0;
            var atoms = ReadGroup(source, ref index, false);
            if (index != source.Length || atoms.Count == 0) throw new InvalidOperationException("Invalid formula: " + formula);
            return new ReadOnlyDictionary<string, int>(atoms);
        }

        private static Dictionary<string, int> ReadGroup(string source, ref int index, bool nested)
        {
            var atoms = new Dictionary<string, int>(StringComparer.Ordinal);
            while (index < source.Length && source[index] != ')')
            {
                if (source[index] == '(')
                {
                    index++;
                    var group = ReadGroup(source, ref index, true);
                    if (group.Count == 0 || index >= source.Length || source[index++] != ')') throw new InvalidOperationException("Unclosed or empty formula group.");
                    Add(atoms, group, ReadCount(source, ref index));
                    continue;
                }
                if (source[index] < 'A' || source[index] > 'Z') throw new InvalidOperationException("Unsupported formula token: " + source);
                var start = index++;
                if (index < source.Length && source[index] >= 'a' && source[index] <= 'z') index++;
                var symbol = source.Substring(start, index - start);
                var count = ReadCount(source, ref index);
                int previous;
                atoms.TryGetValue(symbol, out previous);
                atoms[symbol] = checked(previous + count);
            }
            if (!nested && index < source.Length) throw new InvalidOperationException("Unexpected closing formula group.");
            return atoms;
        }

        private static int ReadCount(string source, ref int index)
        {
            var start = index;
            var count = 0;
            while (index < source.Length && source[index] >= '0' && source[index] <= '9') count = checked(count * 10 + source[index++] - '0');
            if (index == start) return 1;
            if (count <= 0 || source[start] == '0') throw new InvalidOperationException("Invalid formula multiplicity.");
            return count;
        }

        internal static void Add(IDictionary<string, int> target, IEnumerable<KeyValuePair<string, int>> atoms, int multiplier)
        {
            foreach (var atom in atoms)
            {
                int previous;
                target.TryGetValue(atom.Key, out previous);
                target[atom.Key] = checked(previous + atom.Value * multiplier);
            }
        }

        internal static void AssertEqual(IReadOnlyDictionary<string, int> expected, IReadOnlyDictionary<string, int> actual, string context)
        {
            if (expected.Count != actual.Count) throw new InvalidOperationException("Formula changes atom composition: " + context);
            foreach (var atom in expected)
            {
                int count;
                if (!actual.TryGetValue(atom.Key, out count) || count != atom.Value)
                    throw new InvalidOperationException("Formula changes atom composition: " + context);
            }
        }
    }
}
