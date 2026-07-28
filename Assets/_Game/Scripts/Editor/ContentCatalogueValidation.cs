using System;
using System.IO;
using ChemistryLab.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace ChemistryLab.EditorTools
{
    /// <summary>Editor/CI entry point that validates the exact Resources catalogue used at boot.</summary>
    public static class ContentCatalogueValidation
    {
        private const string CataloguePath = "Assets/_Game/Resources/chemistry_catalogue.json";
        private const string ReportPath = "docs/logs/root-catalogue-validation.json";

        [MenuItem("Chemistry Lab/Validate Runtime Catalogue")]
        public static void Validate()
        {
            if (!File.Exists(CataloguePath)) throw new FileNotFoundException("Runtime catalogue was not found.", CataloguePath);

            var catalogue = new ContentImporter().ImportFromJson(File.ReadAllText(CataloguePath));
            var report = new CatalogueValidationReport
            {
                schemaVersion = "1.0",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = UnityEngine.Application.unityVersion,
                result = "succeeded",
                cataloguePath = CataloguePath,
                chemicals = catalogue.ChemicalItems.Count,
                reactions = catalogue.Reactions.Count,
                participants = catalogue.ReactionParticipants.Count
            };
            var absoluteReportPath = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteReportPath));
            File.WriteAllText(
                absoluteReportPath,
                JsonUtility.ToJson(report, true) + Environment.NewLine);
            Debug.Log("[Chemistry Lab] Catalogue valid: " + catalogue.ChemicalItems.Count + " chemicals, "
                + catalogue.Reactions.Count + " reactions, " + catalogue.ReactionParticipants.Count + " participants.");
        }

        [Serializable]
        private sealed class CatalogueValidationReport
        {
            public string schemaVersion;
            public string generatedAtUtc;
            public string unityVersion;
            public string result;
            public string cataloguePath;
            public int chemicals;
            public int reactions;
            public int participants;
        }
    }
}
