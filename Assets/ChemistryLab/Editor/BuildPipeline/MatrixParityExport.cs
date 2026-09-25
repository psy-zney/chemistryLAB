using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChemistryLab.Desktop.Editor
{
    /// <summary>Exports actual C# cells for comparison with the static Pages exporter.</summary>
    public static class MatrixParityExport
    {
        [MenuItem("Chemistry Lab/Desktop/Export 2D Matrix Parity")]
        public static void Export()
        {
            CompoundGenerationMatrix.ValidateOrThrow();
            var source = CompoundGenerationMatrix.Cells;
            var document = new ParityDocument { cells = new ParityCell[source.Count] };
            for (var i = 0; i < source.Count; i++)
            {
                var cell = source[i];
                document.cells[i] = new ParityCell
                {
                    anionId = cell.AnionId, cationId = cell.CationId,
                    formula = cell.FormulaAscii, cationCount = cell.CationCount,
                    anionCount = cell.AnionCount, status = cell.Status
                };
            }
            var directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "matrix-unity-parity.json"), JsonUtility.ToJson(document, true));
            Debug.Log("MATRIX_2D_PARITY_EXPORTED cells=" + source.Count);
        }

        [Serializable] private sealed class ParityDocument { public ParityCell[] cells; }
        [Serializable] private sealed class ParityCell
        {
            public string anionId;
            public string cationId;
            public string formula;
            public int cationCount;
            public int anionCount;
            public string status;
        }
    }
}
