using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// Các luật oxi hóa-khử được mô tả bằng hai bán phản ứng. Bội chung nhỏ nhất
    /// của electron nhường/nhận là kiểm tra bắt buộc trước khi tạo phương trình.
    /// </summary>
    public static class RedoxReactionEngine
    {
        private sealed class RedoxRule
        {
            public string Id;
            public string Name;
            public string ReactantA;
            public double CoefficientA;
            public string ReactantB;
            public double CoefficientB;
            public string Equation;
            public string ProductFormula;
            public double ProductMolarMass;
            public float YieldFraction;
            public ReactionEffect Effect;
            public string ProductColour;
            public float TemperatureDelta;
            public bool RequiresFumeHood;
            public string Observation;
            public string Disposal;
            public int ElectronsLostPerHalfReaction;
            public int ElectronsGainedPerHalfReaction;

            public int ElectronTransferCount
            {
                get
                {
                    return LeastCommonMultiple(
                        ElectronsLostPerHalfReaction,
                        ElectronsGainedPerHalfReaction);
                }
            }

            public ReactionDefinition ToReaction()
            {
                return new ReactionDefinition(
                    Id,
                    Name,
                    ReactantA,
                    CoefficientA,
                    ReactantB,
                    CoefficientB,
                    Equation,
                    ProductFormula,
                    ProductMolarMass,
                    YieldFraction,
                    Effect,
                    ProductColour,
                    TemperatureDelta,
                    RequiresFumeHood,
                    Observation,
                    Disposal);
            }
        }

        private static readonly List<RedoxRule> Rules = BuildRules();

        public static int RuleCount
        {
            get { return Rules.Count; }
        }

        public static bool TryResolve(
            IReadOnlyDictionary<string, double> gramsById,
            ReactionEnvironment environment,
            out ReactionDefinition reaction,
            out string ruleFamily,
            out int electronTransferCount)
        {
            reaction = null;
            ruleFamily = null;
            electronTransferCount = 0;
            if (gramsById == null || gramsById.Count < 2)
            {
                return false;
            }

            for (var index = 0; index < Rules.Count; index++)
            {
                var rule = Rules[index];
                if (!Has(rule.ReactantA, gramsById) || !Has(rule.ReactantB, gramsById))
                {
                    continue;
                }

                if (string.Equals(rule.Id, "redox-copper-nitric-dilute", StringComparison.Ordinal)
                    || string.Equals(rule.Id, "redox-copper-nitric-concentrated", StringComparison.Ordinal))
                {
                    var nitricMolar = ReactionConditionEngine.EstimateConcentration(
                        "nitric-acid",
                        gramsById,
                        environment == null ? .100d : environment.VolumeLitres);
                    var concentrated = nitricMolar >= 8d;
                    if (concentrated != string.Equals(
                            rule.Id,
                            "redox-copper-nitric-concentrated",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }
                }

                reaction = rule.ToReaction();
                ruleFamily = "oxidation-reduction";
                electronTransferCount = rule.ElectronTransferCount;
                return true;
            }

            return false;
        }

        public static void ValidateOrThrow()
        {
            if (Rules.Count < 7)
            {
                throw new InvalidOperationException("Redox engine must contain at least seven reviewed rules.");
            }

            for (var index = 0; index < Rules.Count; index++)
            {
                var rule = Rules[index];
                if (rule.ElectronsLostPerHalfReaction <= 0
                    || rule.ElectronsGainedPerHalfReaction <= 0
                    || rule.ElectronTransferCount <= 0
                    || rule.ElectronTransferCount % rule.ElectronsLostPerHalfReaction != 0
                    || rule.ElectronTransferCount % rule.ElectronsGainedPerHalfReaction != 0)
                {
                    throw new InvalidOperationException(
                        "Invalid electron balance in redox rule " + rule.Id + ".");
                }
            }

            var dilute = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                { "copper", 6.3546d },
                { "nitric-acid", 12.6024d }
            };
            ReactionDefinition reaction;
            string family;
            int electrons;
            if (!TryResolve(
                    dilute,
                    new ReactionEnvironment(24f, .100d),
                    out reaction,
                    out family,
                    out electrons)
                || reaction == null
                || !string.Equals(reaction.ProductFormula, "NO", StringComparison.Ordinal)
                || electrons != 6)
            {
                throw new InvalidOperationException("Dilute nitric-acid redox branch validation failed.");
            }

            if (!TryResolve(
                    dilute,
                    new ReactionEnvironment(24f, .010d),
                    out reaction,
                    out family,
                    out electrons)
                || reaction == null
                || !string.Equals(reaction.ProductFormula, "NO₂", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Concentrated nitric-acid redox branch validation failed.");
            }
        }

        private static List<RedoxRule> BuildRules()
        {
            return new List<RedoxRule>
            {
                new RedoxRule
                {
                    Id = "redox-permanganate-peroxide",
                    Name = "Permanganat oxi hóa hiđro peoxit",
                    ReactantA = "potassium-permanganate",
                    CoefficientA = 2d,
                    ReactantB = "hydrogen-peroxide",
                    CoefficientB = 5d,
                    Equation = "2MnO₄⁻ + 5H₂O₂ + 6H⁺ → 2Mn²⁺ + 5O₂↑ + 8H₂O",
                    ProductFormula = "O₂",
                    ProductMolarMass = 159.990d,
                    YieldFraction = .90f,
                    Effect = ReactionEffect.Gas,
                    ProductColour = "#DDEBEC",
                    TemperatureDelta = 9f,
                    RequiresFumeHood = true,
                    Observation = "Màu tím permanganat mất dần và O₂ thoát ra nhanh trong môi trường axit.",
                    Disposal = "Thực hiện trong tủ hút; tránh nguồn cháy và thu gom dung dịch mangan.",
                    ElectronsLostPerHalfReaction = 2,
                    ElectronsGainedPerHalfReaction = 5
                },
                new RedoxRule
                {
                    Id = "redox-permanganate-chloride",
                    Name = "Permanganat oxi hóa ion clorua",
                    ReactantA = "potassium-permanganate",
                    CoefficientA = 2d,
                    ReactantB = "hydrochloric-acid",
                    CoefficientB = 16d,
                    Equation = "2KMnO₄ + 16HCl → 2KCl + 2MnCl₂ + 5Cl₂↑ + 8H₂O",
                    ProductFormula = "Cl₂",
                    ProductMolarMass = 354.500d,
                    YieldFraction = .86f,
                    Effect = ReactionEffect.Gas,
                    ProductColour = "#B8C95A",
                    TemperatureDelta = 12f,
                    RequiresFumeHood = true,
                    Observation = "Khí Cl₂ vàng lục, độc và ăn mòn được giải phóng.",
                    Disposal = "Bắt buộc dùng tủ hút và bình hấp thụ kiềm; không hít thử.",
                    ElectronsLostPerHalfReaction = 2,
                    ElectronsGainedPerHalfReaction = 5
                },
                new RedoxRule
                {
                    Id = "redox-permanganate-iodide",
                    Name = "Permanganat oxi hóa ion iodua",
                    ReactantA = "potassium-permanganate",
                    CoefficientA = 2d,
                    ReactantB = "potassium-iodide",
                    CoefficientB = 10d,
                    Equation = "2MnO₄⁻ + 10I⁻ + 16H⁺ → 2Mn²⁺ + 5I₂ + 8H₂O",
                    ProductFormula = "I₂",
                    ProductMolarMass = 1269.040d,
                    YieldFraction = .88f,
                    Effect = ReactionEffect.Colour,
                    ProductColour = "#5A2830",
                    TemperatureDelta = 7f,
                    RequiresFumeHood = true,
                    Observation = "Màu tím mất dần, iod nâu tím xuất hiện; hơi iod gây kích ứng.",
                    Disposal = "Dùng tủ hút; khử iod dư bằng thiosunfat trước khi thu gom.",
                    ElectronsLostPerHalfReaction = 2,
                    ElectronsGainedPerHalfReaction = 5
                },
                new RedoxRule
                {
                    Id = "redox-peroxide-iodide",
                    Name = "Hiđro peoxit oxi hóa ion iodua",
                    ReactantA = "hydrogen-peroxide",
                    CoefficientA = 1d,
                    ReactantB = "potassium-iodide",
                    CoefficientB = 2d,
                    Equation = "H₂O₂ + 2I⁻ + 2H⁺ → I₂ + 2H₂O",
                    ProductFormula = "I₂",
                    ProductMolarMass = 253.808d,
                    YieldFraction = .90f,
                    Effect = ReactionEffect.Colour,
                    ProductColour = "#6A3036",
                    TemperatureDelta = 5f,
                    RequiresFumeHood = true,
                    Observation = "Dung dịch chuyển nâu do I₂/I₃⁻ hình thành trong môi trường axit.",
                    Disposal = "Làm việc trong tủ hút và khử iod dư bằng thiosunfat.",
                    ElectronsLostPerHalfReaction = 2,
                    ElectronsGainedPerHalfReaction = 2
                },
                new RedoxRule
                {
                    Id = "redox-copper-nitric-dilute",
                    Name = "Đồng tác dụng axit nitric loãng",
                    ReactantA = "copper",
                    CoefficientA = 3d,
                    ReactantB = "nitric-acid",
                    CoefficientB = 8d,
                    Equation = "3Cu + 8HNO₃(loãng) → 3Cu(NO₃)₂ + 2NO↑ + 4H₂O",
                    ProductFormula = "NO",
                    ProductMolarMass = 60.012d,
                    YieldFraction = .84f,
                    Effect = ReactionEffect.Gas,
                    ProductColour = "#DDEBEC",
                    TemperatureDelta = 11f,
                    RequiresFumeHood = true,
                    Observation = "Khí NO không màu sinh ra rồi bị oxi hóa thành NO₂ nâu trong không khí.",
                    Disposal = "Bắt buộc dùng tủ hút và bình hấp thụ khí nitơ oxit.",
                    ElectronsLostPerHalfReaction = 2,
                    ElectronsGainedPerHalfReaction = 3
                },
                new RedoxRule
                {
                    Id = "redox-copper-nitric-concentrated",
                    Name = "Đồng tác dụng axit nitric đặc",
                    ReactantA = "copper",
                    CoefficientA = 1d,
                    ReactantB = "nitric-acid",
                    CoefficientB = 4d,
                    Equation = "Cu + 4HNO₃(đặc) → Cu(NO₃)₂ + 2NO₂↑ + 2H₂O",
                    ProductFormula = "NO₂",
                    ProductMolarMass = 92.010d,
                    YieldFraction = .88f,
                    Effect = ReactionEffect.Gas,
                    ProductColour = "#A85C35",
                    TemperatureDelta = 15f,
                    RequiresFumeHood = true,
                    Observation = "Khí NO₂ nâu đỏ, độc xuất hiện; dung dịch chuyển xanh do Cu²⁺.",
                    Disposal = "Bắt buộc dùng tủ hút và bình hấp thụ kiềm; tránh mọi phơi nhiễm.",
                    ElectronsLostPerHalfReaction = 2,
                    ElectronsGainedPerHalfReaction = 1
                },
                new RedoxRule
                {
                    Id = "redox-copper-sulfuric",
                    Name = "Đồng tác dụng axit sulfuric đặc nóng",
                    ReactantA = "copper",
                    CoefficientA = 1d,
                    ReactantB = "sulfuric-acid",
                    CoefficientB = 2d,
                    Equation = "Cu + 2H₂SO₄(đặc, nóng) → CuSO₄ + SO₂↑ + 2H₂O",
                    ProductFormula = "SO₂",
                    ProductMolarMass = 64.066d,
                    YieldFraction = .82f,
                    Effect = ReactionEffect.Gas,
                    ProductColour = "#D5DFE2",
                    TemperatureDelta = 18f,
                    RequiresFumeHood = true,
                    Observation = "Đồng tan, dung dịch xanh và khí SO₂ kích ứng thoát ra khi đủ nóng.",
                    Disposal = "Gia nhiệt trong tủ hút, nối bình hấp thụ kiềm và để nguội trước xử lý.",
                    ElectronsLostPerHalfReaction = 2,
                    ElectronsGainedPerHalfReaction = 2
                },
                new RedoxRule
                {
                    Id = "redox-permanganate-iron-two",
                    Name = "Permanganat oxi hóa sắt(II)",
                    ReactantA = "potassium-permanganate",
                    CoefficientA = 2d,
                    ReactantB = "iron-sulfate",
                    CoefficientB = 10d,
                    Equation = "2MnO₄⁻ + 10Fe²⁺ + 16H⁺ → 2Mn²⁺ + 10Fe³⁺ + 8H₂O",
                    ProductFormula = "Fe₂(SO₄)₃",
                    ProductMolarMass = 1999.400d,
                    YieldFraction = .90f,
                    Effect = ReactionEffect.Colour,
                    ProductColour = "#A8783F",
                    TemperatureDelta = 6f,
                    RequiresFumeHood = false,
                    Observation = "Màu tím permanganat biến mất khi Fe²⁺ bị oxi hóa thành Fe³⁺.",
                    Disposal = "Kiểm tra permanganat dư và thu gom dung dịch muối sắt/mangan.",
                    ElectronsLostPerHalfReaction = 1,
                    ElectronsGainedPerHalfReaction = 5
                }
            };
        }

        private static bool Has(
            string chemicalId,
            IReadOnlyDictionary<string, double> gramsById)
        {
            double grams;
            return gramsById.TryGetValue(chemicalId, out grams) && grams > 0d;
        }

        private static int LeastCommonMultiple(int a, int b)
        {
            if (a <= 0 || b <= 0)
            {
                return 0;
            }

            return Math.Abs(a * b) / GreatestCommonDivisor(a, b);
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
    }
}
