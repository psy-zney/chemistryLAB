using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    public enum ChemicalPhase
    {
        Solid,
        Liquid,
        Aqueous,
        Gas
    }

    public enum ChemicalModelKind
    {
        Liquid,
        Crystals,
        Powder,
        Metal
    }

    public enum LabStation
    {
        Workbench,
        FumeHood,
        Sink,
        Storage,
        Analysis
    }

    public enum ReactionEffect
    {
        None,
        Heat,
        Precipitate,
        Gas,
        Colour
    }

    public enum ReactionStatus
    {
        Idle,
        Waiting,
        Reaction,
        Blocked,
        NoMatch
    }

    [Serializable]
    public sealed class ChemicalDefinition
    {
        public ChemicalDefinition(
            string id,
            string name,
            string formula,
            ChemicalPhase phase,
            ChemicalModelKind modelKind,
            double molarMass,
            string density,
            string meltingPoint,
            string boilingPoint,
            string appearance,
            string solubility,
            string hazards,
            string handling,
            string use,
            string modelColour,
            float metallic,
            float smoothness,
            bool transparent)
        {
            Id = id;
            Name = name;
            Formula = formula;
            Phase = phase;
            ModelKind = modelKind;
            MolarMass = molarMass;
            Density = density;
            MeltingPoint = meltingPoint;
            BoilingPoint = boilingPoint;
            Appearance = appearance;
            Solubility = solubility;
            Hazards = hazards;
            Handling = handling;
            Use = use;
            ModelColour = ParseColour(modelColour);
            Metallic = metallic;
            Smoothness = smoothness;
            Transparent = transparent;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Formula { get; private set; }
        public ChemicalPhase Phase { get; private set; }
        public ChemicalModelKind ModelKind { get; private set; }
        public double MolarMass { get; private set; }
        public string Density { get; private set; }
        public string MeltingPoint { get; private set; }
        public string BoilingPoint { get; private set; }
        public string Appearance { get; private set; }
        public string Solubility { get; private set; }
        public string Hazards { get; private set; }
        public string Handling { get; private set; }
        public string Use { get; private set; }
        public Color ModelColour { get; private set; }
        public float Metallic { get; private set; }
        public float Smoothness { get; private set; }
        public bool Transparent { get; private set; }

        public string FamilyLabel
        {
            get
            {
                if (string.Equals(Id, "water", StringComparison.Ordinal))
                {
                    return "Dung môi";
                }

                if (Id.EndsWith("-acid", StringComparison.Ordinal))
                {
                    return "Axit";
                }

                if (Id.EndsWith("-hydroxide", StringComparison.Ordinal)
                    || string.Equals(Id, "ammonia", StringComparison.Ordinal))
                {
                    return "Bazơ";
                }

                if (ModelKind == ChemicalModelKind.Metal)
                {
                    return "Kim loại";
                }

                if (Id.EndsWith("-oxide", StringComparison.Ordinal)
                    || string.Equals(Id, "hydrogen-peroxide", StringComparison.Ordinal))
                {
                    return "Oxit / peoxit";
                }

                return "Muối";
            }
        }

        public string ReactivitySummary
        {
            get
            {
                switch (FamilyLabel)
                {
                    case "Axit":
                        return "Trung hòa bazơ; phản ứng với cacbonat/hiđrocacbonat tạo CO₂; "
                            + "axit không oxi hóa phản ứng với kim loại đứng trước H tạo H₂.";
                    case "Bazơ":
                        return "Trung hòa axit; bazơ tan có thể trao đổi ion với muối để tạo "
                            + "hiđroxit ít tan. Bazơ mạnh phản ứng với muối amoni giải phóng NH₃.";
                    case "Muối":
                        return "Phản ứng trao đổi khi tạo kết tủa, khí hoặc chất điện li yếu; "
                            + "kim loại hoạt động hơn có thể đẩy kim loại yếu hơn khỏi dung dịch muối.";
                    case "Kim loại":
                        return "Khả năng phản ứng phụ thuộc dãy hoạt động; kim loại trước H tác dụng "
                            + "với axit loãng, kim loại mạnh đẩy kim loại yếu khỏi muối.";
                    case "Oxit / peoxit":
                        return "Oxit bazơ phản ứng với axit; một số oxit phản ứng với nước. "
                            + "Peoxit có thể phân hủy, giải phóng O₂ khi có xúc tác.";
                    default:
                        return "Môi trường phản ứng và dung môi phân cực.";
                }
            }
        }

        public string PhaseLabel
        {
            get
            {
                switch (Phase)
                {
                    case ChemicalPhase.Liquid: return "Lỏng";
                    case ChemicalPhase.Aqueous: return "Dung dịch";
                    case ChemicalPhase.Gas: return "Khí";
                    default: return "Rắn";
                }
            }
        }

        private static Color ParseColour(string value)
        {
            Color colour;
            return ColorUtility.TryParseHtmlString(value, out colour) ? colour : Color.magenta;
        }
    }

    [Serializable]
    public sealed class ReactionDefinition
    {
        public ReactionDefinition(
            string id,
            string name,
            string reactantA,
            double coefficientA,
            string reactantB,
            double coefficientB,
            string equation,
            string productFormula,
            double productMolarMass,
            float yieldFraction,
            ReactionEffect effect,
            string productColour,
            float temperatureDelta,
            bool requiresFumeHood,
            string observation,
            string disposal)
        {
            Id = id;
            Name = name;
            ReactantA = reactantA;
            CoefficientA = coefficientA;
            ReactantB = reactantB;
            CoefficientB = coefficientB;
            Equation = equation;
            ProductFormula = productFormula;
            ProductMolarMass = productMolarMass;
            YieldFraction = yieldFraction;
            Effect = effect;
            ProductColour = ParseColour(productColour);
            TemperatureDelta = temperatureDelta;
            RequiresFumeHood = requiresFumeHood;
            Observation = observation;
            Disposal = disposal;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string ReactantA { get; private set; }
        public double CoefficientA { get; private set; }
        public string ReactantB { get; private set; }
        public double CoefficientB { get; private set; }
        public string Equation { get; private set; }
        public string ProductFormula { get; private set; }
        public double ProductMolarMass { get; private set; }
        public float YieldFraction { get; private set; }
        public ReactionEffect Effect { get; private set; }
        public Color ProductColour { get; private set; }
        public float TemperatureDelta { get; private set; }
        public bool RequiresFumeHood { get; private set; }
        public string Observation { get; private set; }
        public string Disposal { get; private set; }

        private static Color ParseColour(string value)
        {
            Color colour;
            return ColorUtility.TryParseHtmlString(value, out colour) ? colour : Color.magenta;
        }
    }

    public sealed class VesselAddition
    {
        public VesselAddition(string chemicalId, double grams)
        {
            ChemicalId = chemicalId;
            Grams = grams;
        }

        public string ChemicalId { get; private set; }
        public double Grams { get; private set; }
    }

    public sealed class ReactionOutcome
    {
        public ReactionStatus Status;
        public ReactionDefinition Reaction;
        public bool GeneratedByRule;
        public string RuleFamily;
        public string Title;
        public string Equation;
        public string Message;
        public string Safety;
        public bool SafetyViolation;
        public AirborneHazard Hazard;
        public double ReleasedGasGrams;
        public string LimitingChemicalId;
        public double TheoreticalProductGrams;
        public double EstimatedProductGrams;
        public float TemperatureC;
        public Color DisplayColour;
        public ReactionEffect Effect;
    }

    public static class DesktopChemistryDatabase
    {
        private static readonly List<ChemicalDefinition> Chemicals = BuildChemicals();
        private static readonly List<ReactionDefinition> Reactions = BuildReactions();
        private static readonly Dictionary<string, ChemicalDefinition> ChemicalById = BuildIndex(Chemicals);

        public static IReadOnlyList<ChemicalDefinition> AllChemicals
        {
            get { return Chemicals; }
        }

        public static IReadOnlyList<ReactionDefinition> AllReactions
        {
            get { return Reactions; }
        }

        public static ChemicalDefinition GetChemical(string id)
        {
            ChemicalDefinition definition;
            return id != null && ChemicalById.TryGetValue(id, out definition) ? definition : null;
        }

        public static ReactionDefinition GetReaction(string id)
        {
            for (var index = 0; index < Reactions.Count; index++)
            {
                if (string.Equals(Reactions[index].Id, id, StringComparison.Ordinal))
                {
                    return Reactions[index];
                }
            }

            return null;
        }

        public static void ValidateOrThrow()
        {
            if (Chemicals.Count < 40)
            {
                throw new InvalidOperationException("Desktop chemistry catalogue must contain at least 40 chemicals.");
            }

            if (Reactions.Count < 35)
            {
                throw new InvalidOperationException("Desktop reaction catalogue must contain at least 35 reactions.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Chemicals.Count; index++)
            {
                var chemical = Chemicals[index];
                if (chemical == null
                    || string.IsNullOrWhiteSpace(chemical.Id)
                    || string.IsNullOrWhiteSpace(chemical.Formula)
                    || chemical.MolarMass <= 0d
                    || !ids.Add(chemical.Id))
                {
                    throw new InvalidOperationException("Invalid or duplicate chemical entry at index " + index + ".");
                }
            }

            var reactionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Reactions.Count; index++)
            {
                var reaction = Reactions[index];
                if (reaction == null
                    || !reactionIds.Add(reaction.Id)
                    || GetChemical(reaction.ReactantA) == null
                    || GetChemical(reaction.ReactantB) == null
                    || reaction.CoefficientA <= 0d
                    || reaction.CoefficientB <= 0d
                    || reaction.ProductMolarMass <= 0d
                    || reaction.YieldFraction <= 0f
                    || reaction.YieldFraction > 1f)
                {
                    throw new InvalidOperationException("Invalid reaction entry at index " + index + ".");
                }
            }
        }

        private static Dictionary<string, ChemicalDefinition> BuildIndex(IEnumerable<ChemicalDefinition> chemicals)
        {
            var result = new Dictionary<string, ChemicalDefinition>(StringComparer.Ordinal);
            foreach (var chemical in chemicals)
            {
                result.Add(chemical.Id, chemical);
            }

            return result;
        }

        private static List<ChemicalDefinition> BuildChemicals()
        {
            var chemicals = new List<ChemicalDefinition>
            {
                new ChemicalDefinition(
                    "water", "Nước cất", "H₂O", ChemicalPhase.Liquid, ChemicalModelKind.Liquid,
                    18.015, "0,998 g/mL · 20 °C", "0 °C", "100 °C",
                    "Chất lỏng trong suốt, không màu", "Trộn lẫn hoàn toàn với nhiều dung môi phân cực",
                    "Nguy cơ thấp trong điều kiện thí nghiệm thông thường",
                    "Giữ dụng cụ sạch để tránh nhiễm ion", "Dung môi và mẫu đối chứng",
                    "#D8F2F4", 0f, 0.82f, true),
                new ChemicalDefinition(
                    "sodium-chloride", "Natri clorua", "NaCl", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    58.443, "2,165 g/cm³", "801 °C", "1.465 °C",
                    "Tinh thể lập phương trắng", "35,9 g/100 mL H₂O · 25 °C",
                    "Nguy cơ thấp; bụi có thể gây kích ứng nhẹ",
                    "Tránh tạo bụi, đậy kín sau khi lấy mẫu", "Chuẩn bị dung dịch clorua",
                    "#F2F1E9", 0f, 0.35f, false),
                new ChemicalDefinition(
                    "hydrochloric-acid", "Axit clohidric", "HCl (aq)", ChemicalPhase.Aqueous, ChemicalModelKind.Liquid,
                    36.461, "Khoảng 1,18 g/mL · dung dịch 37%", "Khoảng −27 °C", "Khoảng 110 °C",
                    "Dung dịch trong suốt, có thể bốc khói", "Trộn lẫn hoàn toàn với nước",
                    "Ăn mòn; hơi gây kích ứng hô hấp",
                    "Dùng kính, găng; thao tác dung dịch đậm đặc trong tủ hút", "Chuẩn độ axit–bazơ",
                    "#F3EAD2", 0f, 0.78f, true),
                new ChemicalDefinition(
                    "sodium-hydroxide", "Natri hiđroxit", "NaOH", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    40.000, "2,13 g/cm³", "318 °C", "1.388 °C",
                    "Viên trắng, hút ẩm mạnh", "Tan mạnh trong nước và tỏa nhiệt",
                    "Ăn mòn mạnh; gây bỏng da và mắt",
                    "Đậy kín, thêm từ từ vào nước; không đổ nước lên khối rắn", "Chuẩn độ bazơ và tạo kết tủa hiđroxit",
                    "#ECEBE4", 0f, 0.38f, false),
                new ChemicalDefinition(
                    "copper-sulfate", "Đồng(II) sunfat pentahiđrat", "CuSO₄·5H₂O", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    249.680, "2,28 g/cm³", "Mất nước từ khoảng 110 °C", "Phân hủy trước khi sôi",
                    "Tinh thể xanh lam", "Khoảng 32 g/100 mL H₂O · 20 °C",
                    "Có hại khi nuốt; nguy hại môi trường nước",
                    "Đeo găng; thu gom vào chất thải kim loại nặng", "Thử phản ứng tạo Cu(OH)₂",
                    "#187FC4", 0f, 0.42f, false),
                new ChemicalDefinition(
                    "sulfuric-acid", "Axit sunfuric", "H₂SO₄", ChemicalPhase.Liquid, ChemicalModelKind.Liquid,
                    98.079, "1,84 g/mL · dung dịch đậm đặc", "10,3 °C", "Khoảng 337 °C",
                    "Chất lỏng không màu, nhớt", "Trộn lẫn với nước và tỏa nhiệt rất mạnh",
                    "Ăn mòn mạnh; phản ứng dữ dội với nước và chất hữu cơ",
                    "Luôn rót axit vào nước, làm việc sau tấm chắn", "Tạo kết tủa sunfat và xúc tác axit",
                    "#E7DEBF", 0f, 0.8f, true),
                new ChemicalDefinition(
                    "potassium-permanganate", "Kali pemanganat", "KMnO₄", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    158.034, "2,70 g/cm³", "Khoảng 240 °C · phân hủy", "Phân hủy",
                    "Tinh thể tím đen", "Khoảng 6,4 g/100 mL H₂O · 20 °C",
                    "Chất oxi hóa; gây bỏng; có hại cho môi trường",
                    "Cách xa chất hữu cơ và chất khử", "Chất oxi hóa trong chuẩn độ",
                    "#50106F", 0f, 0.34f, false),
                new ChemicalDefinition(
                    "barium-chloride", "Bari clorua đihiđrat", "BaCl₂·2H₂O", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    244.260, "3,86 g/cm³", "Khoảng 113 °C · mất nước", "Phân hủy",
                    "Tinh thể trắng", "Tan tốt trong nước",
                    "Độc khi nuốt; muối bari tan cần kiểm soát nghiêm ngặt",
                    "Không chạm tay trần; giữ trong khay độc chất", "Nhận biết ion sunfat",
                    "#EDEDE8", 0f, 0.36f, false),
                new ChemicalDefinition(
                    "silver-nitrate", "Bạc nitrat", "AgNO₃", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    169.873, "4,35 g/cm³", "212 °C", "Khoảng 440 °C · phân hủy",
                    "Tinh thể không màu, nhạy sáng", "Tan nhiều trong nước",
                    "Oxi hóa, ăn mòn; gây tổn thương mắt và nhuộm đen da",
                    "Bảo quản trong chai nâu; thu gom bạc riêng", "Nhận biết halogenua",
                    "#E3E6E0", 0.05f, 0.42f, false),
                new ChemicalDefinition(
                    "potassium-iodide", "Kali iodua", "KI", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    166.003, "3,13 g/cm³", "681 °C", "1.330 °C",
                    "Tinh thể trắng, có thể ngả vàng", "Khoảng 144 g/100 mL H₂O · 25 °C",
                    "Nguy cơ thấp ở lượng thí nghiệm; tránh dùng kéo dài",
                    "Giữ khô và tránh chất oxi hóa mạnh", "Tạo kết tủa PbI₂",
                    "#ECE8D4", 0f, 0.38f, false),
                new ChemicalDefinition(
                    "lead-nitrate", "Chì(II) nitrat", "Pb(NO₃)₂", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    331.209, "4,53 g/cm³", "Khoảng 470 °C · phân hủy", "Phân hủy",
                    "Tinh thể trắng", "Khoảng 52 g/100 mL H₂O · 20 °C",
                    "Độc, chất oxi hóa; nguy hại sinh sản và môi trường",
                    "Chỉ dùng trong khay chì; không tạo bụi; thu gom riêng", "Tạo kết tủa chì iodua",
                    "#E6E4D9", 0f, 0.35f, false),
                new ChemicalDefinition(
                    "iron-chloride", "Sắt(III) clorua hexahiđrat", "FeCl₃·6H₂O", ChemicalPhase.Solid, ChemicalModelKind.Crystals,
                    270.300, "Khoảng 1,82 g/cm³", "Khoảng 37 °C", "Phân hủy",
                    "Tinh thể vàng nâu, hút ẩm", "Tan nhiều trong nước",
                    "Ăn mòn; gây kích ứng và làm ố bề mặt",
                    "Đeo găng, đóng nắp ngay sau khi lấy", "Tạo kết tủa Fe(OH)₃",
                    "#A45C24", 0f, 0.32f, false),
                new ChemicalDefinition(
                    "ammonia", "Dung dịch amoniac", "NH₃ (aq)", ChemicalPhase.Aqueous, ChemicalModelKind.Liquid,
                    17.031, "Khoảng 0,90 g/mL · dung dịch 25%", "Phụ thuộc nồng độ", "Phụ thuộc nồng độ",
                    "Dung dịch không màu, mùi khai mạnh", "Trộn lẫn hoàn toàn với nước",
                    "Ăn mòn; hơi độc và rất kích ứng hô hấp",
                    "Chỉ thao tác trong tủ hút, không hít trực tiếp", "Tạo phức và phản ứng axit–bazơ",
                    "#DCEAF1", 0f, 0.8f, true),
                new ChemicalDefinition(
                    "hydrogen-peroxide", "Hiđro peoxit", "H₂O₂", ChemicalPhase.Liquid, ChemicalModelKind.Liquid,
                    34.015, "1,11 g/mL · dung dịch 30%", "−26 °C · dung dịch 30%", "Khoảng 108 °C",
                    "Chất lỏng trong suốt", "Trộn lẫn hoàn toàn với nước",
                    "Chất oxi hóa; gây bỏng; phân hủy giải phóng O₂",
                    "Tránh ánh sáng, kim loại và tạp chất xúc tác", "Điều chế oxi có kiểm soát",
                    "#D8EDF2", 0f, 0.83f, true),
                new ChemicalDefinition(
                    "acetic-acid", "Axit axetic", "CH₃COOH", ChemicalPhase.Liquid, ChemicalModelKind.Liquid,
                    60.052, "1,049 g/mL", "16,6 °C", "118,1 °C",
                    "Chất lỏng không màu, mùi giấm", "Trộn lẫn hoàn toàn với nước",
                    "Dung dịch đậm đặc ăn mòn và dễ cháy",
                    "Dùng thông gió; tránh nguồn nhiệt", "Chuẩn độ axit yếu",
                    "#E8E0CF", 0f, 0.78f, true),
                new ChemicalDefinition(
                    "calcium-carbonate", "Canxi cacbonat", "CaCO₃", ChemicalPhase.Solid, ChemicalModelKind.Powder,
                    100.087, "2,71 g/cm³", "Phân hủy khoảng 825 °C", "Phân hủy",
                    "Bột trắng", "Hầu như không tan trong nước",
                    "Bụi có thể gây kích ứng cơ học",
                    "Tránh tạo bụi; đậy kín", "Điều chế CO₂ với axit",
                    "#E8E6DC", 0f, 0.25f, false),
                new ChemicalDefinition(
                    "zinc", "Kẽm", "Zn", ChemicalPhase.Solid, ChemicalModelKind.Metal,
                    65.380, "7,14 g/cm³", "419,5 °C", "907 °C",
                    "Kim loại xám xanh", "Không tan trong nước; tan trong axit",
                    "Bụi kẽm dễ cháy; phản ứng với axit tạo H₂",
                    "Dùng mẩu kim loại, tránh bột mịn", "Thử phản ứng kim loại–axit",
                    "#A8B2B1", 0.82f, 0.68f, false),
                new ChemicalDefinition(
                    "copper", "Đồng", "Cu", ChemicalPhase.Solid, ChemicalModelKind.Metal,
                    63.546, "8,96 g/cm³", "1.084,6 °C", "2.562 °C",
                    "Kim loại đỏ cam", "Không tan trong nước",
                    "Nguy cơ thấp dạng khối; bụi và muối đồng cần thu gom riêng",
                    "Giữ bề mặt sạch, tránh phát tán bụi", "Phản ứng thế với AgNO₃",
                    "#B9673D", 0.9f, 0.72f, false),
                new ChemicalDefinition(
                    "magnesium", "Magie", "Mg", ChemicalPhase.Solid, ChemicalModelKind.Metal,
                    24.305, "1,738 g/cm³", "650 °C", "1.091 °C",
                    "Kim loại bạc sáng", "Không tan đáng kể trong nước lạnh",
                    "Dễ cháy dạng dải/bột; phản ứng với axit tạo H₂",
                    "Không dùng nước hoặc CO₂ cho cháy magie", "Điều chế H₂ và minh họa hoạt tính kim loại",
                    "#C8CDD0", 0.86f, 0.76f, false),
                new ChemicalDefinition(
                    "manganese-dioxide", "Mangan(IV) oxit", "MnO₂", ChemicalPhase.Solid, ChemicalModelKind.Powder,
                    86.936, "Khoảng 5,0 g/cm³", "Khoảng 535 °C · phân hủy", "Phân hủy",
                    "Bột nâu đen", "Không tan trong nước",
                    "Có hại khi hít bụi; phơi nhiễm kéo dài ảnh hưởng thần kinh",
                    "Thao tác lượng nhỏ, tránh bụi", "Xúc tác phân hủy H₂O₂",
                    "#342F2A", 0.05f, 0.22f, false)
            };
            chemicals.AddRange(ExtendedChemistryData.BuildChemicals());
            return chemicals;
        }

        private static List<ReactionDefinition> BuildReactions()
        {
            var reactions = new List<ReactionDefinition>
            {
                new ReactionDefinition(
                    "neutralization", "Trung hòa axit–bazơ",
                    "hydrochloric-acid", 1d, "sodium-hydroxide", 1d,
                    "HCl + NaOH → NaCl + H₂O", "H₂O", 18.015, 0.98f,
                    ReactionEffect.Heat, "#E3F0EF", 8f, false,
                    "Dung dịch ấm lên, không tạo kết tủa.", "Trung hòa về pH phù hợp trước khi thải theo quy trình."),
                new ReactionDefinition(
                    "copper-hydroxide", "Đồng(II) hiđroxit",
                    "copper-sulfate", 1d, "sodium-hydroxide", 2d,
                    "CuSO₄ + 2NaOH → Cu(OH)₂↓ + Na₂SO₄", "Cu(OH)₂", 97.561, 0.94f,
                    ReactionEffect.Precipitate, "#48A8D2", 1.2f, false,
                    "Xuất hiện kết tủa xanh lam.", "Thu gom kết tủa vào bình chất thải kim loại nặng."),
                new ReactionDefinition(
                    "barium-sulfate", "Bari sunfat",
                    "barium-chloride", 1d, "sulfuric-acid", 1d,
                    "BaCl₂ + H₂SO₄ → BaSO₄↓ + 2HCl", "BaSO₄", 233.389, 0.97f,
                    ReactionEffect.Precipitate, "#EEECE5", 0.6f, false,
                    "Kết tủa trắng mịn hình thành nhanh.", "Giữ toàn bộ chất thải bari trong bình độc chất."),
                new ReactionDefinition(
                    "silver-chloride", "Bạc clorua",
                    "silver-nitrate", 1d, "sodium-chloride", 1d,
                    "AgNO₃ + NaCl → AgCl↓ + NaNO₃", "AgCl", 143.321, 0.95f,
                    ReactionEffect.Precipitate, "#ECEAE0", 0.4f, false,
                    "Kết tủa trắng sữa, sẫm dần dưới ánh sáng.", "Thu gom vào bình chất thải bạc."),
                new ReactionDefinition(
                    "lead-iodide", "Chì(II) iodua",
                    "lead-nitrate", 1d, "potassium-iodide", 2d,
                    "Pb(NO₃)₂ + 2KI → PbI₂↓ + 2KNO₃", "PbI₂", 461.010, 0.91f,
                    ReactionEffect.Precipitate, "#EACB35", 0.7f, false,
                    "Kết tủa vàng tươi xuất hiện.", "Chất thải chì phải được thu gom riêng."),
                new ReactionDefinition(
                    "iron-hydroxide", "Sắt(III) hiđroxit",
                    "iron-chloride", 1d, "sodium-hydroxide", 3d,
                    "FeCl₃ + 3NaOH → Fe(OH)₃↓ + 3NaCl", "Fe(OH)₃", 106.867, 0.92f,
                    ReactionEffect.Precipitate, "#8A4D2C", 1.0f, false,
                    "Kết tủa nâu đỏ dạng keo.", "Thu gom chất thải kim loại vào đúng bình."),
                new ReactionDefinition(
                    "zinc-hydrogen", "Kẽm và axit clohidric",
                    "zinc", 1d, "hydrochloric-acid", 2d,
                    "Zn + 2HCl → ZnCl₂ + H₂↑", "H₂", 2.016, 0.90f,
                    ReactionEffect.Gas, "#DDEBEA", 9f, true,
                    "Kim loại tan dần, bọt khí H₂ thoát ra.", "Không thử cháy khí; xả qua hệ thống tủ hút."),
                new ReactionDefinition(
                    "magnesium-hydrogen", "Magie và axit clohidric",
                    "magnesium", 1d, "hydrochloric-acid", 2d,
                    "Mg + 2HCl → MgCl₂ + H₂↑", "H₂", 2.016, 0.93f,
                    ReactionEffect.Gas, "#DDEBEA", 14f, true,
                    "Sủi bọt mạnh, magie tan và dung dịch nóng lên.", "Không bịt kín bình sinh khí; dùng tủ hút."),
                new ReactionDefinition(
                    "carbonate-acid", "Canxi cacbonat và axit",
                    "calcium-carbonate", 1d, "hydrochloric-acid", 2d,
                    "CaCO₃ + 2HCl → CaCl₂ + CO₂↑ + H₂O", "CO₂", 44.009, 0.92f,
                    ReactionEffect.Gas, "#D8E7E7", 2f, true,
                    "Sủi bọt CO₂, chất rắn tan dần.", "Xả khí qua tủ hút; trung hòa dung dịch còn lại."),
                new ReactionDefinition(
                    "peroxide-decomposition", "Phân hủy hiđro peoxit",
                    "hydrogen-peroxide", 2d, "manganese-dioxide", 0.02d,
                    "2H₂O₂ —MnO₂→ 2H₂O + O₂↑", "O₂", 31.998, 0.88f,
                    ReactionEffect.Gas, "#D8ECEF", 10f, true,
                    "Bọt O₂ sinh ra nhanh; MnO₂ đóng vai trò xúc tác.", "Không đậy kín; giữ xa chất dễ cháy."),
                new ReactionDefinition(
                    "acetic-neutralization", "Trung hòa axit axetic",
                    "acetic-acid", 1d, "sodium-hydroxide", 1d,
                    "CH₃COOH + NaOH → CH₃COONa + H₂O", "CH₃COONa", 82.034, 0.96f,
                    ReactionEffect.Heat, "#E5ECE9", 5f, false,
                    "Dung dịch ấm lên, mùi axit giảm.", "Kiểm tra pH trước khi xử lý."),
                new ReactionDefinition(
                    "ammonium-chloride", "Amoni clorua",
                    "ammonia", 1d, "hydrochloric-acid", 1d,
                    "NH₃ + HCl → NH₄Cl", "NH₄Cl", 53.491, 0.93f,
                    ReactionEffect.Heat, "#E8ECE6", 6f, true,
                    "Khói trắng NH₄Cl có thể xuất hiện gần miệng bình.", "Chỉ thực hiện trong tủ hút."),
                new ReactionDefinition(
                    "copper-silver", "Phản ứng thế đồng–bạc",
                    "copper", 1d, "silver-nitrate", 2d,
                    "Cu + 2AgNO₃ → Cu(NO₃)₂ + 2Ag↓", "Ag", 215.736, 0.89f,
                    ReactionEffect.Colour, "#B9C2C4", 1.5f, false,
                    "Tinh thể bạc bám lên đồng; dung dịch chuyển xanh.", "Thu hồi bạc và thu gom dung dịch đồng riêng.")
            };
            reactions.AddRange(ExtendedChemistryData.BuildReactions());
            return reactions;
        }
    }

    public static class ReactionSimulator
    {
        public static ReactionOutcome Evaluate(
            IReadOnlyList<VesselAddition> additions,
            LabStation station,
            float baselineTemperatureC)
        {
            var idle = new ReactionOutcome
            {
                Status = additions == null || additions.Count == 0 ? ReactionStatus.Idle : ReactionStatus.Waiting,
                Title = additions == null || additions.Count == 0 ? "Cốc phản ứng sạch" : "Đang chờ chất phản ứng",
                Equation = "—",
                Message = additions == null || additions.Count == 0
                    ? "Chọn một chai trong tủ hóa chất rồi nạp vào cốc."
                    : "Nạp thêm chất phù hợp để mô phỏng phản ứng.",
                Safety = "PPE: kính, áo choàng và găng nitrile.",
                TemperatureC = baselineTemperatureC,
                DisplayColour = LabTheme.Glass,
                Effect = ReactionEffect.None
            };

            if (additions == null || additions.Count < 2)
            {
                return idle;
            }

            var gramsById = new Dictionary<string, double>(StringComparer.Ordinal);
            for (var index = 0; index < additions.Count; index++)
            {
                var addition = additions[index];
                double current;
                gramsById.TryGetValue(addition.ChemicalId, out current);
                gramsById[addition.ChemicalId] = current + Math.Max(0d, addition.Grams);
            }

            ReactionDefinition match = null;
            var generatedByRule = false;
            string ruleFamily = null;
            var rules = DesktopChemistryDatabase.AllReactions;
            for (var index = 0; index < rules.Count; index++)
            {
                var rule = rules[index];
                if (gramsById.ContainsKey(rule.ReactantA) && gramsById.ContainsKey(rule.ReactantB))
                {
                    match = rule;
                    break;
                }
            }

            if (match == null)
            {
                generatedByRule = DynamicReactionEngine.TryResolve(
                    gramsById,
                    out match,
                    out ruleFamily);
            }

            if (match == null)
            {
                idle.Status = ReactionStatus.NoMatch;
                idle.Title = "Không có động lực phản ứng";
                idle.Message =
                    "Không tìm thấy phản ứng mẫu hoặc luật ion/axit–bazơ/thế kim loại phù hợp ở điều kiện hiện tại.";
                idle.Safety = "Hỗn hợp vẫn được giữ lại; không gia nhiệt nếu chưa có luật nhiệt phân.";
                return idle;
            }

            var chemicalA = DesktopChemistryDatabase.GetChemical(match.ReactantA);
            var chemicalB = DesktopChemistryDatabase.GetChemical(match.ReactantB);
            var molesA = gramsById[match.ReactantA] / chemicalA.MolarMass;
            var molesB = gramsById[match.ReactantB] / chemicalB.MolarMass;
            var extentA = molesA / match.CoefficientA;
            var extentB = molesB / match.CoefficientB;
            var extent = Math.Min(extentA, extentB);
            var limiting = extentA <= extentB ? chemicalA : chemicalB;
            var theoreticalMass = extent * match.ProductMolarMass;
            var estimatedMass = theoreticalMass * match.YieldFraction;
            var hazard = match.Effect == ReactionEffect.Gas
                ? AirborneHazardCatalog.Find(match.ProductFormula)
                : null;
            var safetyViolation = match.RequiresFumeHood && station != LabStation.FumeHood;

            return new ReactionOutcome
            {
                Status = ReactionStatus.Reaction,
                Reaction = match,
                GeneratedByRule = generatedByRule,
                RuleFamily = ruleFamily,
                Title = match.Name,
                Equation = match.Equation,
                Message = safetyViolation
                    ? match.Observation + " Phản ứng vẫn xảy ra ngoài tủ hút."
                    : match.Observation,
                Safety = safetyViolation
                    ? "VI PHẠM AN TOÀN: " + match.Disposal
                    : match.Disposal,
                SafetyViolation = safetyViolation,
                Hazard = hazard,
                ReleasedGasGrams = hazard == null ? 0d : estimatedMass,
                LimitingChemicalId = limiting.Id,
                TheoreticalProductGrams = theoreticalMass,
                EstimatedProductGrams = estimatedMass,
                TemperatureC = baselineTemperatureC + match.TemperatureDelta,
                DisplayColour = match.ProductColour,
                Effect = match.Effect
            };
        }
    }

}
