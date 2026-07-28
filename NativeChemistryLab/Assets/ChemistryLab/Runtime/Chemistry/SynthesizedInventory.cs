using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// Lookup hợp nhất: dữ liệu duyệt sẵn có ưu tiên cao nhất, sau đó mới đến
    /// các chất người chơi điều chế trong phiên hoặc khôi phục từ kho JSON.
    /// </summary>
    public static class RuntimeChemicalRegistry
    {
        private static readonly Dictionary<string, ChemicalDefinition> RuntimeById =
            new Dictionary<string, ChemicalDefinition>(StringComparer.Ordinal);

        public static int RuntimeChemicalCount
        {
            get { return RuntimeById.Count; }
        }

        public static ChemicalDefinition GetChemical(string id)
        {
            var curated = DesktopChemistryDatabase.GetChemical(id);
            if (curated != null)
            {
                return curated;
            }

            ChemicalDefinition runtime;
            return id != null && RuntimeById.TryGetValue(id, out runtime) ? runtime : null;
        }

        public static ChemicalDefinition FindByFormula(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                return null;
            }

            var normalised = NormaliseFormula(formula);
            var curated = DesktopChemistryDatabase.AllChemicals;
            for (var index = 0; index < curated.Count; index++)
            {
                if (string.Equals(
                        NormaliseFormula(curated[index].Formula),
                        normalised,
                        StringComparison.Ordinal))
                {
                    return curated[index];
                }
            }

            foreach (var pair in RuntimeById)
            {
                if (string.Equals(
                        NormaliseFormula(pair.Value.Formula),
                        normalised,
                        StringComparison.Ordinal))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        public static ChemicalDefinition RegisterProduct(
            ReactionOutcome outcome,
            SynthesizedBatch restoredBatch = null)
        {
            if (restoredBatch != null)
            {
                var restoredExisting = GetChemical(restoredBatch.ChemicalId);
                if (restoredExisting != null)
                {
                    return restoredExisting;
                }

                var restored = CreateFromBatch(restoredBatch);
                RuntimeById[restored.Id] = restored;
                RegisterWithDynamicEngine(restored, restored.Formula);
                return restored;
            }

            if (outcome == null || outcome.Reaction == null)
            {
                return null;
            }

            var formula = CanonicalProductFormula(outcome.Reaction.ProductFormula);
            var existing = FindByFormula(formula);
            if (existing != null)
            {
                return existing;
            }

            GeneratedCompoundDefinition generated;
            var hasGenerated = CompoundGenerationMatrix.TryFindByFormula(formula, out generated);
            var id = "synth-" + Slug(formula);
            ChemicalDefinition chemical;
            if (hasGenerated)
            {
                chemical = new ChemicalDefinition(
                    id,
                    generated.Name,
                    generated.Formula,
                    generated.Phase,
                    ModelKindFor(generated.Phase),
                    generated.MolarMass,
                    "Ước tính theo họ hợp chất",
                    "Chưa có dữ liệu thực nghiệm",
                    "Chưa có dữ liệu thực nghiệm",
                    generated.Appearance,
                    generated.Solubility.ToString(),
                    HazardText(generated.Hazards),
                    HandlingText(generated.Hazards),
                    "Sản phẩm điều chế; có thể dùng tiếp làm chất phản ứng.",
                    generated.Colour,
                    0f,
                    generated.Phase == ChemicalPhase.Solid ? .32f : .72f,
                    generated.Phase != ChemicalPhase.Solid);
            }
            else
            {
                var phase = outcome.Effect == ReactionEffect.Gas
                    ? ChemicalPhase.Gas
                    : outcome.Effect == ReactionEffect.Precipitate
                        ? ChemicalPhase.Solid
                        : ChemicalPhase.Aqueous;
                chemical = new ChemicalDefinition(
                    id,
                    "Sản phẩm điều chế " + formula,
                    formula,
                    phase,
                    ModelKindFor(phase),
                    ResolveUnitMolarMass(formula, outcome.Reaction.ProductMolarMass),
                    "Chưa xác định",
                    "Chưa xác định",
                    "Chưa xác định",
                    phase == ChemicalPhase.Gas
                        ? "Khí vừa điều chế"
                        : phase == ChemicalPhase.Solid
                            ? "Chất rắn vừa điều chế"
                            : "Dung dịch sản phẩm",
                    phase == ChemicalPhase.Gas ? "—" : "Được giữ trong hỗn hợp phản ứng",
                    outcome.Hazard == null
                        ? HazardText(outcome.ProductHazards)
                        : outcome.Hazard.Warning,
                    outcome.Hazard == null
                        ? HandlingText(outcome.ProductHazards)
                        : "Chỉ thao tác trong tủ hút và hệ dẫn khí kín phù hợp.",
                    "Sản phẩm điều chế; có thể dùng tiếp làm chất phản ứng.",
                    "#" + ColorUtility.ToHtmlStringRGB(outcome.DisplayColour),
                    0f,
                    phase == ChemicalPhase.Solid ? .30f : .70f,
                    phase != ChemicalPhase.Solid);
            }

            RuntimeById[id] = chemical;
            RegisterWithDynamicEngine(chemical, formula);
            return chemical;
        }

        public static void ClearRuntime()
        {
            RuntimeById.Clear();
        }

        public static string NormaliseFormula(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                return string.Empty;
            }

            return formula
                .Replace("₀", "0")
                .Replace("₁", "1")
                .Replace("₂", "2")
                .Replace("₃", "3")
                .Replace("₄", "4")
                .Replace("₅", "5")
                .Replace("₆", "6")
                .Replace("₇", "7")
                .Replace("₈", "8")
                .Replace("₉", "9")
                .Replace("↑", string.Empty)
                .Replace("↓", string.Empty)
                .Replace(" ", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private static ChemicalDefinition CreateFromBatch(SynthesizedBatch batch)
        {
            ChemicalPhase phase;
            if (!Enum.TryParse(batch.Phase, out phase))
            {
                phase = ChemicalPhase.Aqueous;
            }

            return new ChemicalDefinition(
                batch.ChemicalId,
                string.IsNullOrWhiteSpace(batch.Name) ? "Sản phẩm điều chế " + batch.Formula : batch.Name,
                batch.Formula,
                phase,
                ModelKindFor(phase),
                Math.Max(.001d, batch.MolarMass),
                "Khôi phục từ lô đã lưu",
                "Xem dữ liệu nguồn phản ứng",
                "Xem dữ liệu nguồn phản ứng",
                string.IsNullOrWhiteSpace(batch.Appearance) ? "Sản phẩm điều chế" : batch.Appearance,
                "Theo dữ liệu lô điều chế",
                string.IsNullOrWhiteSpace(batch.HazardText) ? "Chưa phân loại" : batch.HazardText,
                "Đánh giá lại nguy cơ trước khi tái sử dụng.",
                "Tái sử dụng trong engine phản ứng.",
                string.IsNullOrWhiteSpace(batch.Colour) ? "#E8ECE8" : batch.Colour,
                0f,
                phase == ChemicalPhase.Solid ? .32f : .72f,
                phase != ChemicalPhase.Solid);
        }

        private static void RegisterWithDynamicEngine(ChemicalDefinition chemical, string formula)
        {
            GeneratedCompoundDefinition generated;
            if (chemical != null
                && CompoundGenerationMatrix.TryFindByFormula(formula, out generated))
            {
                DynamicReactionEngine.RegisterGeneratedSpecies(chemical.Id, generated);
            }
        }

        private static ChemicalModelKind ModelKindFor(ChemicalPhase phase)
        {
            return phase == ChemicalPhase.Solid
                ? ChemicalModelKind.Crystals
                : ChemicalModelKind.Liquid;
        }

        private static string Slug(string formula)
        {
            var normalised = NormaliseFormula(formula).ToLowerInvariant();
            var builder = new StringBuilder(normalised.Length);
            for (var index = 0; index < normalised.Length; index++)
            {
                var character = normalised[index];
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
            }

            return builder.Length == 0 ? "product" : builder.ToString();
        }

        private static double ResolveUnitMolarMass(string formula, double fallback)
        {
            GeneratedCompoundDefinition generated;
            if (CompoundGenerationMatrix.TryFindByFormula(formula, out generated))
            {
                return generated.MolarMass;
            }

            var masses = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                { "H2", 2.016d },
                { "O2", 31.998d },
                { "CO2", 44.009d },
                { "NH3", 17.031d },
                { "H2S", 34.081d },
                { "CL2", 70.900d },
                { "I2", 253.808d },
                { "NO", 30.006d },
                { "NO2", 46.005d },
                { "SO2", 64.066d },
                { "AG", 107.868d }
            };
            double mass;
            return masses.TryGetValue(NormaliseFormula(formula), out mass)
                ? mass
                : Math.Max(.001d, fallback);
        }

        private static string HazardText(ChemicalHazardFlags hazards)
        {
            return hazards == ChemicalHazardFlags.None
                ? "Chưa ghi nhận cờ nguy hại từ dữ liệu suy diễn."
                : "Cờ nguy hại: " + hazards + ".";
        }

        private static string CanonicalProductFormula(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                return string.Empty;
            }

            formula = formula.Replace("↑", string.Empty).Replace("↓", string.Empty).Trim();
            var firstFormulaCharacter = 0;
            while (firstFormulaCharacter < formula.Length
                && char.IsDigit(formula[firstFormulaCharacter]))
            {
                firstFormulaCharacter++;
            }

            return firstFormulaCharacter > 0 && firstFormulaCharacter < formula.Length
                ? formula.Substring(firstFormulaCharacter)
                : formula;
        }

        private static string HandlingText(ChemicalHazardFlags hazards)
        {
            return hazards == ChemicalHazardFlags.None
                ? "Đeo kính, áo choàng và găng; xác minh trước khi dùng quy mô lớn."
                : "Dùng PPE phù hợp, tủ hút khi có độc/ăn mòn và thu gom riêng.";
        }
    }

    [Serializable]
    public sealed class SynthesizedBatch
    {
        public string BatchId;
        public string ChemicalId;
        public string Name;
        public string Formula;
        public string Phase;
        public double MolarMass;
        public double AvailableGrams;
        public float PurityFraction;
        public string Colour;
        public string Appearance;
        public string HazardText;
        public string SourceReactionId;
        public string SourceEquation;
        public string CreatedUtc;
    }

    [Serializable]
    internal sealed class SynthesizedInventoryDocument
    {
        public int SchemaVersion = 1;
        public List<SynthesizedBatch> Batches = new List<SynthesizedBatch>();
    }

    /// <summary>
    /// Kho lô sản phẩm được lưu bằng JSON. Khối lượng được trừ thật khi người chơi
    /// nạp lại vào bình, nên cùng một sản phẩm không thể được nhân bản vô hạn.
    /// </summary>
    public sealed class SynthesizedInventory
    {
        private readonly string savePath;
        private readonly List<SynthesizedBatch> batches = new List<SynthesizedBatch>();

        public SynthesizedInventory(string customSavePath = null)
        {
            savePath = string.IsNullOrWhiteSpace(customSavePath)
                ? Path.Combine(Application.persistentDataPath, "chemistry-inventory.json")
                : customSavePath;
        }

        public IReadOnlyList<SynthesizedBatch> Batches
        {
            get { return batches; }
        }

        public int Count
        {
            get { return batches.Count; }
        }

        public string SavePath
        {
            get { return savePath; }
        }

        public void Load()
        {
            batches.Clear();
            if (!File.Exists(savePath))
            {
                return;
            }

            try
            {
                var document = JsonUtility.FromJson<SynthesizedInventoryDocument>(
                    File.ReadAllText(savePath));
                if (document == null || document.Batches == null)
                {
                    return;
                }

                for (var index = 0; index < document.Batches.Count; index++)
                {
                    var batch = document.Batches[index];
                    if (batch == null
                        || string.IsNullOrWhiteSpace(batch.BatchId)
                        || string.IsNullOrWhiteSpace(batch.ChemicalId)
                        || string.IsNullOrWhiteSpace(batch.Formula)
                        || batch.AvailableGrams <= .0001d)
                    {
                        continue;
                    }

                    batches.Add(batch);
                    RuntimeChemicalRegistry.RegisterProduct(null, batch);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Không thể đọc chemistry-inventory.json; bắt đầu với kho trống. "
                    + exception.Message);
            }
        }

        public SynthesizedBatch AddProduct(ReactionOutcome outcome)
        {
            if (outcome == null
                || outcome.Status != ReactionStatus.Reaction
                || outcome.Reaction == null
                || !outcome.CanCollectProduct
                || outcome.EstimatedProductGrams <= .0001d)
            {
                return null;
            }

            var chemical = RuntimeChemicalRegistry.RegisterProduct(outcome);
            if (chemical == null)
            {
                return null;
            }

            var batch = new SynthesizedBatch
            {
                BatchId = Guid.NewGuid().ToString("N"),
                ChemicalId = chemical.Id,
                Name = chemical.Name,
                Formula = chemical.Formula,
                Phase = chemical.Phase.ToString(),
                MolarMass = chemical.MolarMass,
                AvailableGrams = outcome.EstimatedProductGrams,
                PurityFraction = outcome.ProductPurity,
                Colour = "#" + ColorUtility.ToHtmlStringRGB(chemical.ModelColour),
                Appearance = chemical.Appearance,
                HazardText = chemical.Hazards,
                SourceReactionId = outcome.Reaction.Id,
                SourceEquation = outcome.Equation,
                CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            batches.Add(batch);
            Save();
            return batch;
        }

        public bool TryConsume(string batchId, double requestedGrams, out double consumedGrams)
        {
            consumedGrams = 0d;
            var batch = Find(batchId);
            if (batch == null || requestedGrams <= 0d || batch.AvailableGrams <= 0d)
            {
                return false;
            }

            consumedGrams = Math.Min(requestedGrams, batch.AvailableGrams);
            batch.AvailableGrams = Math.Max(0d, batch.AvailableGrams - consumedGrams);
            if (batch.AvailableGrams <= .0001d)
            {
                batches.Remove(batch);
            }

            Save();
            return consumedGrams > 0d;
        }

        public SynthesizedBatch Find(string batchId)
        {
            for (var index = 0; index < batches.Count; index++)
            {
                if (string.Equals(batches[index].BatchId, batchId, StringComparison.Ordinal))
                {
                    return batches[index];
                }
            }

            return null;
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var document = new SynthesizedInventoryDocument
                {
                    SchemaVersion = 1,
                    Batches = new List<SynthesizedBatch>(batches)
                };
                File.WriteAllText(
                    savePath,
                    JsonUtility.ToJson(document, true) + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Không thể lưu kho hóa chất JSON. " + exception.Message);
            }
        }

        public static void ValidateOrThrow()
        {
            var temporaryPath = Path.Combine(
                Application.temporaryCachePath,
                "chemistry-inventory-validation.json");
            var inventory = new SynthesizedInventory(temporaryPath);
            var reaction = new ReactionDefinition(
                "inventory-validation",
                "Inventory validation",
                "hydrochloric-acid",
                1d,
                "sodium-hydroxide",
                1d,
                "HCl + NaOH → NaCl + H₂O",
                "NaCl",
                58.440d,
                .9f,
                ReactionEffect.Heat,
                "#E8ECE8",
                1f,
                false,
                "Validation",
                "Validation");
            var outcome = new ReactionOutcome
            {
                Status = ReactionStatus.Reaction,
                Reaction = reaction,
                Equation = reaction.Equation,
                EstimatedProductGrams = 4.4d,
                ProductPurity = .9f,
                CanCollectProduct = true,
                DisplayColour = Color.white,
                Effect = ReactionEffect.Heat
            };
            var batch = inventory.AddProduct(outcome);
            double consumed;
            if (batch == null
                || !inventory.TryConsume(batch.BatchId, 1.4d, out consumed)
                || Math.Abs(consumed - 1.4d) > .001d
                || Math.Abs(batch.AvailableGrams - 3d) > .001d)
            {
                throw new InvalidOperationException("Synthesized inventory mass accounting validation failed.");
            }

            var runtimeReaction = new ReactionDefinition(
                "runtime-species-validation",
                "Runtime species validation",
                "nitric-acid",
                2d,
                "calcium-hydroxide",
                1d,
                "2HNO₃ + Ca(OH)₂ → Ca(NO₃)₂ + 2H₂O",
                "Ca(NO₃)₂",
                164.086d,
                .9f,
                ReactionEffect.Heat,
                "#E8ECE8",
                1f,
                false,
                "Validation",
                "Validation");
            var runtimeOutcome = new ReactionOutcome
            {
                Status = ReactionStatus.Reaction,
                Reaction = runtimeReaction,
                Equation = runtimeReaction.Equation,
                EstimatedProductGrams = 4d,
                ProductPurity = .88f,
                CanCollectProduct = true,
                DisplayColour = Color.white,
                Effect = ReactionEffect.Heat
            };
            var speciesBefore = DynamicReactionEngine.SupportedSpeciesCount;
            var runtimeBatch = inventory.AddProduct(runtimeOutcome);
            var runtimeChemical = runtimeBatch == null
                ? null
                : RuntimeChemicalRegistry.GetChemical(runtimeBatch.ChemicalId);
            if (runtimeBatch == null
                || runtimeChemical == null
                || DynamicReactionEngine.SupportedSpeciesCount != speciesBefore + 1)
            {
                throw new InvalidOperationException(
                    "Synthesized product runtime-species registration validation failed.");
            }

            DynamicReactionEngine.UnregisterGeneratedSpecies(runtimeBatch.ChemicalId);
            RuntimeChemicalRegistry.ClearRuntime();

            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // A locked validation artifact is harmless and remains outside source control.
            }
        }
    }
}
