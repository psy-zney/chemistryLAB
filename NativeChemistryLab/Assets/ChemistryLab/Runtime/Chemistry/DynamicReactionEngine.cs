using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// Rule-based high-school chemistry fallback. Curated reactions remain authoritative;
    /// this engine is used only when no reviewed catalogue entry matches the mixture.
    /// </summary>
    public static class DynamicReactionEngine
    {
        private enum SpeciesKind
        {
            Inert,
            Acid,
            Base,
            Ammonia,
            Salt,
            Carbonate,
            Bicarbonate,
            Sulfide,
            AmmoniumSalt,
            Metal,
            BasicOxide
        }

        private sealed class Ion
        {
            public Ion(string id, string formula, int charge, double molarMass, string colour)
            {
                Id = id;
                Formula = formula;
                Charge = charge;
                MolarMass = molarMass;
                Colour = colour;
            }

            public string Id;
            public string Formula;
            public int Charge;
            public double MolarMass;
            public string Colour;
        }

        private sealed class Species
        {
            public Species(
                string id,
                string formula,
                SpeciesKind kind,
                Ion cation,
                Ion anion,
                int acidicHydrogens,
                int hydroxides,
                int activity,
                bool oxidisingAcid)
            {
                Id = id;
                Formula = formula;
                Kind = kind;
                Cation = cation;
                Anion = anion;
                AcidicHydrogens = acidicHydrogens;
                Hydroxides = hydroxides;
                Activity = activity;
                OxidisingAcid = oxidisingAcid;
            }

            public string Id;
            public string Formula;
            public SpeciesKind Kind;
            public Ion Cation;
            public Ion Anion;
            public int AcidicHydrogens;
            public int Hydroxides;
            public int Activity;
            public bool OxidisingAcid;
        }

        private sealed class FormulaUnit
        {
            public Ion Cation;
            public Ion Anion;
            public int CationCount;
            public int AnionCount;
            public string Formula;
            public double MolarMass;
        }

        private static readonly Ion Sodium = Cation("sodium", "Na", 1, 22.990, "#EEEDE7");
        private static readonly Ion Potassium = Cation("potassium", "K", 1, 39.098, "#EEEDE7");
        private static readonly Ion Calcium = Cation("calcium", "Ca", 2, 40.078, "#EEEDE7");
        private static readonly Ion Barium = Cation("barium", "Ba", 2, 137.327, "#EEEDE7");
        private static readonly Ion CopperTwo = Cation("copper-two", "Cu", 2, 63.546, "#3D9A9D");
        private static readonly Ion Silver = Cation("silver", "Ag", 1, 107.868, "#E7E6DD");
        private static readonly Ion Lead = Cation("lead", "Pb", 2, 207.200, "#E4DFC8");
        private static readonly Ion IronThree = Cation("iron-three", "Fe", 3, 55.845, "#A76535");
        private static readonly Ion IronTwo = Cation("iron-two", "Fe", 2, 55.845, "#7F9E82");
        private static readonly Ion Aluminium = Cation("aluminium", "Al", 3, 26.982, "#EEEDE7");
        private static readonly Ion Ammonium = Cation("ammonium", "NH₄", 1, 18.039, "#EEEDE7");
        private static readonly Ion Zinc = Cation("zinc", "Zn", 2, 65.380, "#E5E7E3");
        private static readonly Ion Magnesium = Cation("magnesium", "Mg", 2, 24.305, "#E8E9E5");

        private static readonly Ion Chloride = Anion("chloride", "Cl", 1, 35.450);
        private static readonly Ion Sulfate = Anion("sulfate", "SO₄", 2, 96.056);
        private static readonly Ion Nitrate = Anion("nitrate", "NO₃", 1, 62.004);
        private static readonly Ion Iodide = Anion("iodide", "I", 1, 126.904);
        private static readonly Ion Bromide = Anion("bromide", "Br", 1, 79.904);
        private static readonly Ion Acetate = Anion("acetate", "CH₃COO", 1, 59.044);
        private static readonly Ion Carbonate = Anion("carbonate", "CO₃", 2, 60.008);
        private static readonly Ion Bicarbonate = Anion("bicarbonate", "HCO₃", 1, 61.016);
        private static readonly Ion Phosphate = Anion("phosphate", "PO₄", 3, 94.971);
        private static readonly Ion Sulfide = Anion("sulfide", "S", 2, 32.065);
        private static readonly Ion Permanganate = Anion("permanganate", "MnO₄", 1, 118.936);

        private static readonly Dictionary<string, Species> SpeciesById = BuildSpecies();

        public static int SupportedSpeciesCount
        {
            get { return SpeciesById.Count; }
        }

        public static int RuleFamilyCount
        {
            get { return 9; }
        }

        public static bool TryResolve(
            IReadOnlyDictionary<string, double> gramsById,
            out ReactionDefinition reaction,
            out string ruleFamily)
        {
            reaction = null;
            ruleFamily = null;
            if (gramsById == null || gramsById.Count < 2)
            {
                return false;
            }

            var ids = new List<string>();
            foreach (var pair in gramsById)
            {
                if (pair.Value > 0d && SpeciesById.ContainsKey(pair.Key))
                {
                    ids.Add(pair.Key);
                }
            }

            ids.Sort(StringComparer.Ordinal);
            for (var left = 0; left < ids.Count - 1; left++)
            {
                for (var right = left + 1; right < ids.Count; right++)
                {
                    var a = SpeciesById[ids[left]];
                    var b = SpeciesById[ids[right]];
                    if (TryResolvePair(a, b, out reaction, out ruleFamily))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void ValidateOrThrow()
        {
            if (SpeciesById.Count != DesktopChemistryDatabase.AllChemicals.Count)
            {
                throw new InvalidOperationException(
                    "Dynamic reaction species coverage does not match the chemical catalogue. species="
                    + SpeciesById.Count + " chemicals=" + DesktopChemistryDatabase.AllChemicals.Count);
            }

            ValidateGenerated(
                "acid-base",
                new[] { "nitric-acid", "calcium-hydroxide" },
                "Ca(NO₃)₂");
            ValidateGenerated(
                "precipitation",
                new[] { "calcium-chloride", "sodium-phosphate" },
                "Ca₃(PO₄)₂");
            ValidateGenerated(
                "metal-acid",
                new[] { "iron", "phosphoric-acid" },
                "H₂");
            ValidateGenerated(
                "metal-displacement",
                new[] { "magnesium", "iron-sulfate" },
                "Fe");
        }

        private static void ValidateGenerated(string family, string[] ids, string product)
        {
            var mixture = new Dictionary<string, double>(StringComparer.Ordinal);
            for (var index = 0; index < ids.Length; index++)
            {
                mixture.Add(ids[index], 10d);
            }

            ReactionDefinition reaction;
            string actualFamily;
            if (!TryResolve(mixture, out reaction, out actualFamily)
                || reaction == null
                || !string.Equals(family, actualFamily, StringComparison.Ordinal)
                || reaction.Equation.IndexOf(product, StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    "Dynamic reaction validation failed for family " + family + ".");
            }
        }

        private static bool TryResolvePair(
            Species a,
            Species b,
            out ReactionDefinition reaction,
            out string family)
        {
            if (TryKinds(a, b, SpeciesKind.Acid, SpeciesKind.Base, out a, out b))
            {
                reaction = BuildAcidBase(a, b);
                family = "acid-base";
                return reaction != null;
            }

            if (TryKinds(a, b, SpeciesKind.Acid, SpeciesKind.Ammonia, out a, out b))
            {
                reaction = BuildAcidAmmonia(a, b);
                family = "acid-base";
                return reaction != null;
            }

            if (a.Kind == SpeciesKind.Acid
                && (b.Kind == SpeciesKind.Carbonate || b.Kind == SpeciesKind.Bicarbonate)
                || b.Kind == SpeciesKind.Acid
                && (a.Kind == SpeciesKind.Carbonate || a.Kind == SpeciesKind.Bicarbonate))
            {
                var acid = a.Kind == SpeciesKind.Acid ? a : b;
                var carbonate = a.Kind == SpeciesKind.Acid ? b : a;
                reaction = BuildAcidGasSalt(acid, carbonate, "CO₂", 44.009, "acid-carbonate");
                family = carbonate.Kind == SpeciesKind.Bicarbonate
                    ? "acid-bicarbonate"
                    : "acid-carbonate";
                return reaction != null;
            }

            if (TryKinds(a, b, SpeciesKind.Acid, SpeciesKind.Sulfide, out a, out b))
            {
                reaction = BuildAcidGasSalt(a, b, "H₂S", 34.081, "acid-sulfide");
                family = "acid-sulfide";
                return reaction != null;
            }

            if (a.Kind == SpeciesKind.AmmoniumSalt && b.Kind == SpeciesKind.Base
                || b.Kind == SpeciesKind.AmmoniumSalt && a.Kind == SpeciesKind.Base)
            {
                var ammoniumSalt = a.Kind == SpeciesKind.AmmoniumSalt ? a : b;
                var baseSpecies = a.Kind == SpeciesKind.Base ? a : b;
                reaction = BuildAmmoniumBase(ammoniumSalt, baseSpecies);
                family = "ammonium-base";
                return reaction != null;
            }

            if (IsExchangeSalt(a) && IsExchangeSalt(b))
            {
                reaction = BuildPrecipitation(a, b);
                if (reaction != null)
                {
                    family = "precipitation";
                    return true;
                }
            }

            if (a.Kind == SpeciesKind.Metal && IsExchangeSalt(b)
                || b.Kind == SpeciesKind.Metal && IsExchangeSalt(a))
            {
                var metal = a.Kind == SpeciesKind.Metal ? a : b;
                var salt = a.Kind == SpeciesKind.Metal ? b : a;
                reaction = BuildMetalDisplacement(metal, salt);
                if (reaction != null)
                {
                    family = "metal-displacement";
                    return true;
                }
            }

            if (a.Kind == SpeciesKind.Metal && b.Kind == SpeciesKind.Acid
                || b.Kind == SpeciesKind.Metal && a.Kind == SpeciesKind.Acid)
            {
                var metal = a.Kind == SpeciesKind.Metal ? a : b;
                var acid = a.Kind == SpeciesKind.Acid ? a : b;
                reaction = BuildMetalAcid(metal, acid);
                if (reaction != null)
                {
                    family = "metal-acid";
                    return true;
                }
            }

            if (TryKinds(a, b, SpeciesKind.Acid, SpeciesKind.BasicOxide, out a, out b))
            {
                reaction = BuildBasicOxideAcid(a, b);
                family = "basic-oxide-acid";
                return reaction != null;
            }

            reaction = null;
            family = null;
            return false;
        }

        private static ReactionDefinition BuildAcidBase(Species acid, Species basis)
        {
            var salt = MakeFormula(basis.Cation, acid.Anion);
            var acidCoefficient = salt.AnionCount;
            var baseCoefficient = salt.CationCount;
            var waterCoefficient = acidCoefficient * acid.AcidicHydrogens;
            if (waterCoefficient != baseCoefficient * basis.Hydroxides)
            {
                return null;
            }

            return Create(
                "acid-base",
                "Trung hòa suy diễn",
                acid,
                acidCoefficient,
                basis,
                baseCoefficient,
                Term(acidCoefficient, acid.Formula) + " + " + Term(baseCoefficient, basis.Formula)
                + " → " + salt.Formula + " + " + Term(waterCoefficient, "H₂O"),
                salt.Formula,
                salt.MolarMass,
                ReactionEffect.Heat,
                salt.Cation.Colour,
                6f + waterCoefficient * 1.4f,
                false,
                "Axit và bazơ trung hòa; nhiệt độ tăng, tạo muối " + salt.Formula + " và nước.",
                "Để nguội, kiểm tra pH và phân loại ion kim loại trước khi xử lý.");
        }

        private static ReactionDefinition BuildAcidAmmonia(Species acid, Species ammonia)
        {
            var salt = MakeFormula(Ammonium, acid.Anion);
            var acidCoefficient = salt.AnionCount;
            var ammoniaCoefficient = salt.CationCount;
            if (acidCoefficient * acid.AcidicHydrogens != ammoniaCoefficient)
            {
                return null;
            }

            return Create(
                "acid-ammonia",
                "Axit tác dụng với amoniac",
                acid,
                acidCoefficient,
                ammonia,
                ammoniaCoefficient,
                Term(acidCoefficient, acid.Formula) + " + " + Term(ammoniaCoefficient, "NH₃")
                + " → " + salt.Formula,
                salt.Formula,
                salt.MolarMass,
                ReactionEffect.Heat,
                salt.Cation.Colour,
                5f,
                true,
                "Amoniac bị giữ lại dưới dạng muối amoni " + salt.Formula + ".",
                "Thực hiện trong tủ hút vì dung dịch amoniac có thể phát tán hơi kích ứng.");
        }

        private static ReactionDefinition BuildAcidGasSalt(
            Species acid,
            Species gasSalt,
            string gasFormula,
            double gasMolarMass,
            string id)
        {
            var salt = MakeFormula(gasSalt.Cation, acid.Anion);
            var gasHydrogens = gasSalt.Kind == SpeciesKind.Bicarbonate ? 1 : 2;
            int acidCoefficient;
            int sourceCoefficient;
            int saltCoefficient;
            if (!BalanceAcidSaltGas(
                    acid,
                    gasSalt,
                    salt,
                    gasHydrogens,
                    out acidCoefficient,
                    out sourceCoefficient,
                    out saltCoefficient))
            {
                return null;
            }

            var sourceFormulaUnit = MakeFormula(gasSalt.Cation, gasSalt.Anion);
            var gasCoefficient = sourceCoefficient * sourceFormulaUnit.AnionCount;
            return Create(
                id,
                gasFormula == "H₂S" ? "Giải phóng hiđro sunfua" : "Axit giải phóng cacbon đioxit",
                acid,
                acidCoefficient,
                gasSalt,
                sourceCoefficient,
                Term(acidCoefficient, acid.Formula) + " + " + Term(sourceCoefficient, gasSalt.Formula)
                + " → " + Term(saltCoefficient, salt.Formula) + " + "
                + Term(gasCoefficient, gasFormula) + "↑"
                + (gasFormula == "CO₂" ? " + " + Term(gasCoefficient, "H₂O") : string.Empty),
                gasFormula,
                gasMolarMass * gasCoefficient,
                ReactionEffect.Gas,
                gasFormula == "H₂S" ? "#D4DDC4" : "#DDEBEC",
                gasFormula == "H₂S" ? 3f : 2f,
                true,
                gasFormula == "H₂S"
                    ? "Khí H₂S không màu, cực độc được giải phóng."
                    : "Sủi bọt CO₂; chất rắn hoặc muối axit tan dần.",
                gasFormula == "H₂S"
                    ? "Cảnh báo độc cấp tính: dùng tủ hút và nối bình hấp thụ khí."
                    : "Không đậy kín bình sinh khí; thông gió hoặc nối bình cách ly.");
        }

        private static ReactionDefinition BuildAmmoniumBase(Species ammoniumSalt, Species basis)
        {
            var productSalt = MakeFormula(basis.Cation, ammoniumSalt.Anion);
            var ammoniumUnit = MakeFormula(Ammonium, ammoniumSalt.Anion);
            var ammoniumCoefficient = productSalt.AnionCount;
            var baseCoefficient = productSalt.CationCount;
            var gasCoefficient = ammoniumCoefficient * ammoniumUnit.CationCount;
            if (gasCoefficient != baseCoefficient * basis.Hydroxides)
            {
                return null;
            }

            return Create(
                "ammonium-base",
                "Bazơ giải phóng amoniac",
                ammoniumSalt,
                ammoniumCoefficient,
                basis,
                baseCoefficient,
                Term(ammoniumCoefficient, ammoniumSalt.Formula) + " + " + Term(baseCoefficient, basis.Formula)
                + " → " + productSalt.Formula
                + " + " + Term(gasCoefficient, "NH₃") + "↑ + " + Term(gasCoefficient, "H₂O"),
                "NH₃",
                17.031 * gasCoefficient,
                ReactionEffect.Gas,
                "#DCE9EC",
                5f,
                true,
                "Khí NH₃ không màu và rất kích ứng được giải phóng.",
                "Dùng tủ hút, mặt nạ phù hợp và nối bình hấp thụ axit loãng.");
        }

        private static ReactionDefinition BuildPrecipitation(Species a, Species b)
        {
            if (a.Cation == null || a.Anion == null || b.Cation == null || b.Anion == null
                || a.Cation.Id == b.Cation.Id || a.Anion.Id == b.Anion.Id)
            {
                return null;
            }

            var productA = MakeFormula(a.Cation, b.Anion);
            var productB = MakeFormula(b.Cation, a.Anion);
            var insolubleA = IsInsoluble(productA);
            var insolubleB = IsInsoluble(productB);
            if (!insolubleA && !insolubleB)
            {
                return null;
            }

            int coefficientA;
            int coefficientB;
            int coefficientProductA;
            int coefficientProductB;
            if (!BalanceExchange(
                    MakeFormula(a.Cation, a.Anion),
                    MakeFormula(b.Cation, b.Anion),
                    productA,
                    productB,
                    out coefficientA,
                    out coefficientB,
                    out coefficientProductA,
                    out coefficientProductB))
            {
                return null;
            }

            var precipitate = insolubleA ? productA : productB;
            var precipitateCoefficient = insolubleA ? coefficientProductA : coefficientProductB;
            return Create(
                "precipitation",
                "Phản ứng trao đổi tạo kết tủa",
                a,
                coefficientA,
                b,
                coefficientB,
                Term(coefficientA, a.Formula) + " + " + Term(coefficientB, b.Formula)
                + " → " + Term(coefficientProductA, productA.Formula)
                + (insolubleA ? "↓" : string.Empty) + " + "
                + Term(coefficientProductB, productB.Formula)
                + (insolubleB ? "↓" : string.Empty),
                precipitate.Formula,
                precipitate.MolarMass * precipitateCoefficient,
                ReactionEffect.Precipitate,
                precipitate.Cation.Colour,
                .6f,
                false,
                "Xuất hiện kết tủa " + precipitate.Formula + " theo quy tắc độ tan.",
                IsHeavyMetal(precipitate.Cation)
                    ? "Lọc và thu gom kết tủa vào bình chất thải kim loại nặng."
                    : "Lọc chất rắn và kiểm tra ion còn dư trước khi xử lý.");
        }

        private static ReactionDefinition BuildMetalDisplacement(Species metal, Species salt)
        {
            if (metal.Cation == null || salt.Cation == null || salt.Anion == null
                || metal.Activity <= ActivityForCation(salt.Cation))
            {
                return null;
            }

            var productSalt = MakeFormula(metal.Cation, salt.Anion);
            var sourceSalt = MakeFormula(salt.Cation, salt.Anion);
            int metalCoefficient;
            int saltCoefficient;
            int productCoefficient;
            int displacedCoefficient;
            if (!BalanceDisplacement(
                    productSalt,
                    sourceSalt,
                    out metalCoefficient,
                    out saltCoefficient,
                    out productCoefficient,
                    out displacedCoefficient))
            {
                return null;
            }

            return Create(
                "metal-displacement",
                "Kim loại đẩy kim loại yếu hơn",
                metal,
                metalCoefficient,
                salt,
                saltCoefficient,
                Term(metalCoefficient, metal.Formula) + " + " + Term(saltCoefficient, salt.Formula)
                + " → " + Term(productCoefficient, productSalt.Formula)
                + " + " + Term(displacedCoefficient, salt.Cation.Formula) + "↓",
                salt.Cation.Formula,
                salt.Cation.MolarMass * displacedCoefficient,
                ReactionEffect.Colour,
                salt.Cation.Colour,
                3f,
                false,
                salt.Cation.Formula + " bám trên " + metal.Formula + "; màu dung dịch thay đổi.",
                "Thu gom cả kim loại và dung dịch chứa ion kim loại vào đúng bình.");
        }

        private static ReactionDefinition BuildMetalAcid(Species metal, Species acid)
        {
            if (metal.Activity <= 0 || acid.OxidisingAcid || metal.Cation == null)
            {
                return null;
            }

            var salt = MakeFormula(metal.Cation, acid.Anion);
            int metalCoefficient;
            int acidCoefficient;
            int saltCoefficient;
            int hydrogenCoefficient;
            if (!BalanceMetalAcid(
                    metal,
                    acid,
                    salt,
                    out metalCoefficient,
                    out acidCoefficient,
                    out saltCoefficient,
                    out hydrogenCoefficient))
            {
                return null;
            }

            return Create(
                "metal-acid",
                "Kim loại tác dụng với axit",
                metal,
                metalCoefficient,
                acid,
                acidCoefficient,
                Term(metalCoefficient, metal.Formula) + " + " + Term(acidCoefficient, acid.Formula)
                + " → " + Term(saltCoefficient, salt.Formula)
                + " + " + Term(hydrogenCoefficient, "H₂") + "↑",
                "H₂",
                2.016 * hydrogenCoefficient,
                ReactionEffect.Gas,
                "#DDEBEA",
                8f,
                true,
                "Kim loại tan dần và giải phóng khí H₂ dễ cháy.",
                "Không tạo tia lửa, không đậy kín; dùng tủ hút hoặc dẫn khí vào bình cách ly.");
        }

        private static ReactionDefinition BuildBasicOxideAcid(Species acid, Species oxide)
        {
            var salt = MakeFormula(oxide.Cation, acid.Anion);
            var acidCoefficient = salt.AnionCount;
            var oxideCoefficient = salt.CationCount;
            var waterCoefficient = acidCoefficient * acid.AcidicHydrogens / 2;
            if (acidCoefficient * acid.AcidicHydrogens != oxideCoefficient * 2)
            {
                return null;
            }

            return Create(
                "basic-oxide-acid",
                "Oxit bazơ tác dụng với axit",
                acid,
                acidCoefficient,
                oxide,
                oxideCoefficient,
                Term(acidCoefficient, acid.Formula) + " + " + Term(oxideCoefficient, oxide.Formula)
                + " → " + salt.Formula + " + " + Term(waterCoefficient, "H₂O"),
                salt.Formula,
                salt.MolarMass,
                ReactionEffect.Heat,
                salt.Cation.Colour,
                9f,
                false,
                "Oxit tan dần, tạo muối " + salt.Formula + " và nước; hỗn hợp nóng lên.",
                "Để nguội và kiểm tra pH trước khi xử lý.");
        }

        private static ReactionDefinition Create(
            string family,
            string name,
            Species a,
            int coefficientA,
            Species b,
            int coefficientB,
            string equation,
            string product,
            double productMass,
            ReactionEffect effect,
            string colour,
            float temperatureDelta,
            bool hood,
            string observation,
            string disposal)
        {
            return new ReactionDefinition(
                "generated-" + family + "-" + a.Id + "-" + b.Id,
                name + " · luật động",
                a.Id,
                coefficientA,
                b.Id,
                coefficientB,
                equation,
                product,
                productMass,
                .90f,
                effect,
                colour,
                temperatureDelta,
                hood,
                observation,
                disposal);
        }

        private static bool TryKinds(
            Species a,
            Species b,
            SpeciesKind expectedA,
            SpeciesKind expectedB,
            out Species orderedA,
            out Species orderedB)
        {
            if (a.Kind == expectedA && b.Kind == expectedB)
            {
                orderedA = a;
                orderedB = b;
                return true;
            }

            if (a.Kind == expectedB && b.Kind == expectedA)
            {
                orderedA = b;
                orderedB = a;
                return true;
            }

            orderedA = a;
            orderedB = b;
            return false;
        }

        private static bool IsExchangeSalt(Species species)
        {
            return species.Kind == SpeciesKind.Salt
                || species.Kind == SpeciesKind.Carbonate
                || species.Kind == SpeciesKind.Bicarbonate
                || species.Kind == SpeciesKind.Sulfide
                || species.Kind == SpeciesKind.AmmoniumSalt;
        }

        private static bool IsInsoluble(FormulaUnit salt)
        {
            if (salt.Cation.Id == Sodium.Id
                || salt.Cation.Id == Potassium.Id
                || salt.Cation.Id == Ammonium.Id
                || salt.Anion.Id == Nitrate.Id)
            {
                return false;
            }

            if (salt.Anion.Id == Chloride.Id || salt.Anion.Id == Bromide.Id || salt.Anion.Id == Iodide.Id)
            {
                return salt.Cation.Id == Silver.Id || salt.Cation.Id == Lead.Id;
            }

            if (salt.Anion.Id == Sulfate.Id)
            {
                return salt.Cation.Id == Barium.Id
                    || salt.Cation.Id == Lead.Id
                    || salt.Cation.Id == Calcium.Id;
            }

            if (salt.Anion.Id == Carbonate.Id
                || salt.Anion.Id == Phosphate.Id
                || salt.Anion.Id == Sulfide.Id)
            {
                return true;
            }

            return false;
        }

        private static bool IsHeavyMetal(Ion cation)
        {
            return cation.Id == Barium.Id
                || cation.Id == CopperTwo.Id
                || cation.Id == Silver.Id
                || cation.Id == Lead.Id;
        }

        private static int ActivityForCation(Ion cation)
        {
            if (cation.Id == Magnesium.Id) return 70;
            if (cation.Id == Aluminium.Id) return 60;
            if (cation.Id == Zinc.Id) return 50;
            if (cation.Id == IronTwo.Id || cation.Id == IronThree.Id) return 40;
            if (cation.Id == CopperTwo.Id) return -10;
            if (cation.Id == Silver.Id) return -20;
            return 100;
        }

        private static FormulaUnit MakeFormula(Ion cation, Ion anion)
        {
            var divisor = GreatestCommonDivisor(cation.Charge, anion.Charge);
            var cationCount = anion.Charge / divisor;
            var anionCount = cation.Charge / divisor;
            return new FormulaUnit
            {
                Cation = cation,
                Anion = anion,
                CationCount = cationCount,
                AnionCount = anionCount,
                Formula = FormatIon(cation.Formula, cationCount) + FormatIon(anion.Formula, anionCount),
                MolarMass = cation.MolarMass * cationCount + anion.MolarMass * anionCount
            };
        }

        private static string FormatIon(string formula, int count)
        {
            if (count <= 1)
            {
                return formula;
            }

            var polyatomic = formula == "NH₄"
                || formula == "SO₄"
                || formula == "NO₃"
                || formula == "CH₃COO"
                || formula == "CO₃"
                || formula == "HCO₃"
                || formula == "PO₄"
                || formula == "MnO₄";
            return (polyatomic ? "(" + formula + ")" : formula) + Subscript(count);
        }

        private static string Term(int coefficient, string formula)
        {
            return coefficient <= 1 ? formula : coefficient + formula;
        }

        private static string Subscript(int value)
        {
            var source = value.ToString();
            var result = new StringBuilder(source.Length);
            const string subscripts = "₀₁₂₃₄₅₆₇₈₉";
            for (var index = 0; index < source.Length; index++)
            {
                result.Append(subscripts[source[index] - '0']);
            }

            return result.ToString();
        }

        private static bool BalanceAcidSaltGas(
            Species acid,
            Species source,
            FormulaUnit product,
            int gasHydrogens,
            out int acidCoefficient,
            out int sourceCoefficient,
            out int productCoefficient)
        {
            var sourceUnit = MakeFormula(source.Cation, source.Anion);
            for (var x = 1; x <= 12; x++)
            {
                for (var y = 1; y <= 12; y++)
                {
                    for (var z = 1; z <= 12; z++)
                    {
                        if (x * acid.AcidicHydrogens == y * sourceUnit.AnionCount * gasHydrogens
                            && y * sourceUnit.CationCount == z * product.CationCount
                            && x == z * product.AnionCount)
                        {
                            acidCoefficient = x;
                            sourceCoefficient = y;
                            productCoefficient = z;
                            return true;
                        }
                    }
                }
            }

            acidCoefficient = sourceCoefficient = productCoefficient = 0;
            return false;
        }

        private static bool BalanceExchange(
            FormulaUnit a,
            FormulaUnit b,
            FormulaUnit productA,
            FormulaUnit productB,
            out int coefficientA,
            out int coefficientB,
            out int coefficientProductA,
            out int coefficientProductB)
        {
            for (var x = 1; x <= 8; x++)
            for (var y = 1; y <= 8; y++)
            for (var z = 1; z <= 8; z++)
            for (var w = 1; w <= 8; w++)
            {
                if (x * a.CationCount == z * productA.CationCount
                    && x * a.AnionCount == w * productB.AnionCount
                    && y * b.CationCount == w * productB.CationCount
                    && y * b.AnionCount == z * productA.AnionCount)
                {
                    coefficientA = x;
                    coefficientB = y;
                    coefficientProductA = z;
                    coefficientProductB = w;
                    return true;
                }
            }

            coefficientA = coefficientB = coefficientProductA = coefficientProductB = 0;
            return false;
        }

        private static bool BalanceDisplacement(
            FormulaUnit product,
            FormulaUnit source,
            out int metalCoefficient,
            out int sourceCoefficient,
            out int productCoefficient,
            out int displacedCoefficient)
        {
            for (var x = 1; x <= 8; x++)
            for (var y = 1; y <= 8; y++)
            for (var z = 1; z <= 8; z++)
            {
                if (x == z * product.CationCount
                    && y * source.AnionCount == z * product.AnionCount)
                {
                    metalCoefficient = x;
                    sourceCoefficient = y;
                    productCoefficient = z;
                    displacedCoefficient = y * source.CationCount;
                    return true;
                }
            }

            metalCoefficient = sourceCoefficient = productCoefficient = displacedCoefficient = 0;
            return false;
        }

        private static bool BalanceMetalAcid(
            Species metal,
            Species acid,
            FormulaUnit salt,
            out int metalCoefficient,
            out int acidCoefficient,
            out int saltCoefficient,
            out int hydrogenCoefficient)
        {
            for (var x = 1; x <= 12; x++)
            for (var y = 1; y <= 12; y++)
            for (var z = 1; z <= 12; z++)
            {
                var hydrogens = y * acid.AcidicHydrogens;
                if (hydrogens % 2 == 0
                    && x == z * salt.CationCount
                    && y == z * salt.AnionCount)
                {
                    metalCoefficient = x;
                    acidCoefficient = y;
                    saltCoefficient = z;
                    hydrogenCoefficient = hydrogens / 2;
                    return true;
                }
            }

            metalCoefficient = acidCoefficient = saltCoefficient = hydrogenCoefficient = 0;
            return false;
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

        private static Ion Cation(string id, string formula, int charge, double mass, string colour)
        {
            return new Ion(id, formula, charge, mass, colour);
        }

        private static Ion Anion(string id, string formula, int charge, double mass)
        {
            return new Ion(id, formula, charge, mass, "#E8ECE8");
        }

        private static Dictionary<string, Species> BuildSpecies()
        {
            var result = new Dictionary<string, Species>(StringComparer.Ordinal);
            Add(result, "water", "H₂O", SpeciesKind.Inert);
            Add(result, "sodium-chloride", "NaCl", SpeciesKind.Salt, Sodium, Chloride);
            AddAcid(result, "hydrochloric-acid", "HCl", Chloride, 1, false);
            AddBase(result, "sodium-hydroxide", "NaOH", Sodium, 1);
            Add(result, "copper-sulfate", "CuSO₄", SpeciesKind.Salt, CopperTwo, Sulfate);
            AddAcid(result, "sulfuric-acid", "H₂SO₄", Sulfate, 2, false);
            Add(result, "potassium-permanganate", "KMnO₄", SpeciesKind.Salt, Potassium, Permanganate);
            Add(result, "barium-chloride", "BaCl₂", SpeciesKind.Salt, Barium, Chloride);
            Add(result, "silver-nitrate", "AgNO₃", SpeciesKind.Salt, Silver, Nitrate);
            Add(result, "potassium-iodide", "KI", SpeciesKind.Salt, Potassium, Iodide);
            Add(result, "lead-nitrate", "Pb(NO₃)₂", SpeciesKind.Salt, Lead, Nitrate);
            Add(result, "iron-chloride", "FeCl₃", SpeciesKind.Salt, IronThree, Chloride);
            Add(result, "ammonia", "NH₃", SpeciesKind.Ammonia);
            Add(result, "hydrogen-peroxide", "H₂O₂", SpeciesKind.Inert);
            AddAcid(result, "acetic-acid", "CH₃COOH", Acetate, 1, false);
            Add(result, "calcium-carbonate", "CaCO₃", SpeciesKind.Carbonate, Calcium, Carbonate);
            AddMetal(result, "zinc", "Zn", Zinc, 50);
            AddMetal(result, "copper", "Cu", CopperTwo, -10);
            AddMetal(result, "magnesium", "Mg", Magnesium, 70);
            Add(result, "manganese-dioxide", "MnO₂", SpeciesKind.Inert);

            AddAcid(result, "nitric-acid", "HNO₃", Nitrate, 1, true);
            AddAcid(result, "phosphoric-acid", "H₃PO₄", Phosphate, 3, false);
            AddBase(result, "potassium-hydroxide", "KOH", Potassium, 1);
            AddBase(result, "calcium-hydroxide", "Ca(OH)₂", Calcium, 2);
            AddBase(result, "barium-hydroxide", "Ba(OH)₂", Barium, 2);
            Add(result, "sodium-carbonate", "Na₂CO₃", SpeciesKind.Carbonate, Sodium, Carbonate);
            Add(result, "sodium-bicarbonate", "NaHCO₃", SpeciesKind.Bicarbonate, Sodium, Bicarbonate);
            Add(result, "calcium-chloride", "CaCl₂", SpeciesKind.Salt, Calcium, Chloride);
            Add(result, "copper-chloride", "CuCl₂", SpeciesKind.Salt, CopperTwo, Chloride);
            Add(result, "iron-sulfate", "FeSO₄", SpeciesKind.Salt, IronTwo, Sulfate);
            Add(result, "aluminium-chloride", "AlCl₃", SpeciesKind.Salt, Aluminium, Chloride);
            Add(result, "ammonium-chloride", "NH₄Cl", SpeciesKind.AmmoniumSalt, Ammonium, Chloride);
            Add(result, "sodium-sulfate", "Na₂SO₄", SpeciesKind.Salt, Sodium, Sulfate);
            Add(result, "potassium-nitrate", "KNO₃", SpeciesKind.Salt, Potassium, Nitrate);
            Add(result, "sodium-sulfide", "Na₂S", SpeciesKind.Sulfide, Sodium, Sulfide);
            AddMetal(result, "aluminium", "Al", Aluminium, 60);
            AddMetal(result, "iron", "Fe", IronTwo, 40);
            Add(result, "sodium-phosphate", "Na₃PO₄", SpeciesKind.Salt, Sodium, Phosphate);
            Add(result, "calcium-oxide", "CaO", SpeciesKind.BasicOxide, Calcium, null);
            Add(result, "potassium-bromide", "KBr", SpeciesKind.Salt, Potassium, Bromide);
            return result;
        }

        private static void Add(
            IDictionary<string, Species> result,
            string id,
            string formula,
            SpeciesKind kind,
            Ion cation = null,
            Ion anion = null)
        {
            result.Add(id, new Species(id, formula, kind, cation, anion, 0, 0, 0, false));
        }

        private static void AddAcid(
            IDictionary<string, Species> result,
            string id,
            string formula,
            Ion anion,
            int hydrogens,
            bool oxidising)
        {
            result.Add(id, new Species(
                id, formula, SpeciesKind.Acid, null, anion, hydrogens, 0, 0, oxidising));
        }

        private static void AddBase(
            IDictionary<string, Species> result,
            string id,
            string formula,
            Ion cation,
            int hydroxides)
        {
            result.Add(id, new Species(
                id, formula, SpeciesKind.Base, cation, null, 0, hydroxides, 0, false));
        }

        private static void AddMetal(
            IDictionary<string, Species> result,
            string id,
            string formula,
            Ion cation,
            int activity)
        {
            result.Add(id, new Species(
                id, formula, SpeciesKind.Metal, cation, null, 0, 0, activity, false));
        }
    }
}
