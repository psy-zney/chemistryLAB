using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    public enum ElementCategory
    {
        AlkaliMetal,
        AlkalineEarth,
        TransitionMetal,
        PostTransitionMetal,
        Metalloid,
        Nonmetal,
        Halogen,
        NobleGas,
        Actinide
    }

    [Serializable]
    public sealed class PeriodicElementDefinition
    {
        public PeriodicElementDefinition(
            int atomicNumber,
            string symbol,
            string name,
            double atomicMass,
            int period,
            int group,
            ElementCategory category,
            string electronConfiguration,
            string phase,
            string appearance,
            string density,
            string meltingPoint,
            string boilingPoint,
            string oxidationStates,
            string chemicalProperties,
            string occurrence,
            string colour)
        {
            AtomicNumber = atomicNumber;
            Symbol = symbol;
            Name = name;
            AtomicMass = atomicMass;
            Period = period;
            Group = group;
            Category = category;
            ElectronConfiguration = electronConfiguration;
            Phase = phase;
            Appearance = appearance;
            Density = density;
            MeltingPoint = meltingPoint;
            BoilingPoint = boilingPoint;
            OxidationStates = oxidationStates;
            ChemicalProperties = chemicalProperties;
            Occurrence = occurrence;
            Color parsed;
            ModelColour = ColorUtility.TryParseHtmlString(colour, out parsed) ? parsed : Color.magenta;
        }

        public int AtomicNumber { get; private set; }
        public string Symbol { get; private set; }
        public string Name { get; private set; }
        public double AtomicMass { get; private set; }
        public int Period { get; private set; }
        public int Group { get; private set; }
        public ElementCategory Category { get; private set; }
        public string ElectronConfiguration { get; private set; }
        public string Phase { get; private set; }
        public string Appearance { get; private set; }
        public string Density { get; private set; }
        public string MeltingPoint { get; private set; }
        public string BoilingPoint { get; private set; }
        public string OxidationStates { get; private set; }
        public string ChemicalProperties { get; private set; }
        public string Occurrence { get; private set; }
        public Color ModelColour { get; private set; }

        public string CategoryLabel
        {
            get
            {
                switch (Category)
                {
                    case ElementCategory.AlkaliMetal: return "Kim loại kiềm";
                    case ElementCategory.AlkalineEarth: return "Kim loại kiềm thổ";
                    case ElementCategory.TransitionMetal: return "Kim loại chuyển tiếp";
                    case ElementCategory.PostTransitionMetal: return "Kim loại sau chuyển tiếp";
                    case ElementCategory.Metalloid: return "Á kim";
                    case ElementCategory.Halogen: return "Halogen";
                    case ElementCategory.NobleGas: return "Khí hiếm";
                    case ElementCategory.Actinide: return "Họ actini";
                    default: return "Phi kim";
                }
            }
        }
    }

    public static class HighSchoolPeriodicTable
    {
        private static readonly List<PeriodicElementDefinition> Elements = BuildElements();
        private static readonly Dictionary<int, PeriodicElementDefinition> ByAtomicNumber = BuildIndex();

        public static IReadOnlyList<PeriodicElementDefinition> All
        {
            get { return Elements; }
        }

        public static PeriodicElementDefinition Get(int atomicNumber)
        {
            PeriodicElementDefinition element;
            return ByAtomicNumber.TryGetValue(atomicNumber, out element) ? element : null;
        }

        public static void ValidateOrThrow()
        {
            if (Elements.Count < 45)
            {
                throw new InvalidOperationException("Bảng tuần hoàn THPT phải có ít nhất 45 nguyên tố.");
            }

            var symbols = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Elements.Count; index++)
            {
                var element = Elements[index];
                if (element.AtomicNumber <= 0
                    || string.IsNullOrWhiteSpace(element.Symbol)
                    || element.AtomicMass <= 0d
                    || !symbols.Add(element.Symbol))
                {
                    throw new InvalidOperationException("Dữ liệu nguyên tố không hợp lệ tại vị trí " + index + ".");
                }
            }
        }

        public static Color CategoryColour(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.AlkaliMetal: return Hex("#E66A4E");
                case ElementCategory.AlkalineEarth: return Hex("#E7A24A");
                case ElementCategory.TransitionMetal: return Hex("#58899A");
                case ElementCategory.PostTransitionMetal: return Hex("#75837C");
                case ElementCategory.Metalloid: return Hex("#4E8C79");
                case ElementCategory.Halogen: return Hex("#A46AA8");
                case ElementCategory.NobleGas: return Hex("#5575A8");
                case ElementCategory.Actinide: return Hex("#9A6655");
                default: return Hex("#678C62");
            }
        }

        private static Dictionary<int, PeriodicElementDefinition> BuildIndex()
        {
            var result = new Dictionary<int, PeriodicElementDefinition>();
            for (var index = 0; index < Elements.Count; index++)
            {
                result.Add(Elements[index].AtomicNumber, Elements[index]);
            }

            return result;
        }

        private static PeriodicElementDefinition E(
            int number,
            string symbol,
            string name,
            double mass,
            int period,
            int group,
            ElementCategory category,
            string configuration,
            string phase,
            string appearance,
            string density,
            string melting,
            string boiling,
            string oxidation,
            string chemistry,
            string occurrence,
            string colour)
        {
            return new PeriodicElementDefinition(
                number, symbol, name, mass, period, group, category, configuration,
                phase, appearance, density, melting, boiling, oxidation, chemistry, occurrence, colour);
        }

        private static List<PeriodicElementDefinition> BuildElements()
        {
            const ElementCategory alkali = ElementCategory.AlkaliMetal;
            const ElementCategory earth = ElementCategory.AlkalineEarth;
            const ElementCategory transition = ElementCategory.TransitionMetal;
            const ElementCategory post = ElementCategory.PostTransitionMetal;
            const ElementCategory metalloid = ElementCategory.Metalloid;
            const ElementCategory nonmetal = ElementCategory.Nonmetal;
            const ElementCategory halogen = ElementCategory.Halogen;
            const ElementCategory noble = ElementCategory.NobleGas;

            return new List<PeriodicElementDefinition>
            {
                E(1, "H", "Hiđro", 1.008, 1, 1, nonmetal, "1s¹", "Khí", "Không màu", "0,0899 g/L", "−259,2 °C", "−252,9 °C", "−1, +1", "Cháy trong O₂ tạo H₂O; khử được một số oxit kim loại.", "Nước, hợp chất hữu cơ và khí quyển lượng vết.", "#D9EEF0"),
                E(2, "He", "Heli", 4.0026, 1, 18, noble, "1s²", "Khí", "Không màu", "0,1785 g/L", "Không đông ở 1 atm", "−268,9 °C", "0", "Trơ hóa học trong điều kiện thường.", "Khí thiên nhiên và sản phẩm phân rã phóng xạ.", "#E9D7A8"),
                E(3, "Li", "Liti", 6.94, 2, 1, alkali, "[He] 2s¹", "Rắn", "Kim loại bạc mềm", "0,534 g/cm³", "180,5 °C", "1.342 °C", "+1", "Tác dụng với nước và halogen; hoạt động kém Na, K.", "Khoáng spodumen, lepidolit và nước muối.", "#B7BDC2"),
                E(4, "Be", "Beri", 9.0122, 2, 2, earth, "[He] 2s²", "Rắn", "Kim loại xám thép", "1,85 g/cm³", "1.287 °C", "2.469 °C", "+2", "Oxit và hiđroxit lưỡng tính; bột và hợp chất độc.", "Khoáng berin.", "#AEB8BA"),
                E(5, "B", "Bo", 10.81, 2, 13, metalloid, "[He] 2s² 2p¹", "Rắn", "Á kim nâu đen", "2,34 g/cm³", "2.076 °C", "Khoảng 3.927 °C", "+3", "Bền ở thường; tạo B₂O₃ và các borat.", "Borax và quặng kernit.", "#3F3B37"),
                E(6, "C", "Cacbon", 12.011, 2, 14, nonmetal, "[He] 2s² 2p²", "Rắn", "Than chì đen hoặc kim cương trong suốt", "2,27 g/cm³ · than chì", "Thăng hoa khoảng 3.642 °C", "Thăng hoa", "−4, +2, +4", "Cháy tạo CO/CO₂; là chất khử quan trọng ở nhiệt độ cao.", "Than, cacbonat và mọi hợp chất hữu cơ.", "#262626"),
                E(7, "N", "Nitơ", 14.007, 2, 15, nonmetal, "[He] 2s² 2p³", "Khí", "Không màu", "1,251 g/L", "−210,0 °C", "−195,8 °C", "−3 đến +5", "N₂ khá trơ; phản ứng với H₂ ở xúc tác, nhiệt độ và áp suất cao.", "Khoảng 78% thể tích khí quyển.", "#B8D6E8"),
                E(8, "O", "Oxi", 15.999, 2, 16, nonmetal, "[He] 2s² 2p⁴", "Khí", "Không màu", "1,429 g/L", "−218,8 °C", "−183,0 °C", "−2; −1 trong peoxit", "Chất oxi hóa, duy trì sự cháy và hô hấp.", "Khoảng 21% khí quyển; phổ biến trong oxit và nước.", "#AFC9EE"),
                E(9, "F", "Flo", 18.998, 2, 17, halogen, "[He] 2s² 2p⁵", "Khí", "Vàng nhạt", "1,696 g/L", "−219,7 °C", "−188,1 °C", "−1", "Phi kim oxi hóa mạnh nhất; phản ứng với hầu hết nguyên tố.", "Chỉ gặp trong hợp chất như fluorit.", "#DDE66B"),
                E(10, "Ne", "Neon", 20.180, 2, 18, noble, "[He] 2s² 2p⁶", "Khí", "Không màu", "0,900 g/L", "−248,6 °C", "−246,1 °C", "0", "Trơ hóa học; phát sáng đỏ cam khi phóng điện.", "Lượng vết trong khí quyển.", "#E4A6A1"),
                E(11, "Na", "Natri", 22.990, 3, 1, alkali, "[Ne] 3s¹", "Rắn", "Kim loại bạc mềm", "0,968 g/cm³", "97,8 °C", "883 °C", "+1", "Phản ứng mãnh liệt với nước tạo NaOH và H₂; bảo quản trong dầu.", "Muối mỏ, nước biển; không tồn tại tự do.", "#C9CED1"),
                E(12, "Mg", "Magie", 24.305, 3, 2, earth, "[Ne] 3s²", "Rắn", "Kim loại bạc sáng", "1,738 g/cm³", "650 °C", "1.091 °C", "+2", "Cháy sáng trắng tạo MgO; tác dụng với axit loãng giải phóng H₂.", "Dolomit, magiezit và nước biển.", "#D6DADD"),
                E(13, "Al", "Nhôm", 26.982, 3, 13, post, "[Ne] 3s² 3p¹", "Rắn", "Kim loại trắng bạc", "2,70 g/cm³", "660,3 °C", "2.470 °C", "+3", "Có màng Al₂O₃ bảo vệ; Al₂O₃ và Al(OH)₃ lưỡng tính.", "Quặng bôxit.", "#CBD0D2"),
                E(14, "Si", "Silic", 28.085, 3, 14, metalloid, "[Ne] 3s² 3p²", "Rắn", "Á kim xám xanh", "2,33 g/cm³", "1.414 °C", "3.265 °C", "−4, +4", "Bền ở thường; phản ứng với halogen, kiềm và O₂ khi đun nóng.", "Silicat và SiO₂ trong cát, thạch anh.", "#697477"),
                E(15, "P", "Photpho", 30.974, 3, 15, nonmetal, "[Ne] 3s² 3p³", "Rắn", "Trắng sáp hoặc đỏ sẫm", "1,82 g/cm³ · P trắng", "44,1 °C · P trắng", "280,5 °C", "−3, +3, +5", "P trắng rất hoạt động và độc; cháy tạo P₂O₅/P₄O₁₀.", "Quặng photphorit và apatit.", "#E6DAD0"),
                E(16, "S", "Lưu huỳnh", 32.06, 3, 16, nonmetal, "[Ne] 3s² 3p⁴", "Rắn", "Tinh thể vàng", "2,07 g/cm³", "115,2 °C", "444,7 °C", "−2, +4, +6", "Cháy xanh tạo SO₂; phản ứng với kim loại tạo sunfua.", "Mỏ lưu huỳnh, sunfua và sunfat.", "#E0C52B"),
                E(17, "Cl", "Clo", 35.45, 3, 17, halogen, "[Ne] 3s² 3p⁵", "Khí", "Vàng lục", "3,214 g/L", "−101,5 °C", "−34,0 °C", "−1, +1, +3, +5, +7", "Oxi hóa mạnh; tác dụng với H₂ và nhiều kim loại; độc.", "Muối clorua, đặc biệt NaCl.", "#B7CB54"),
                E(18, "Ar", "Argon", 39.948, 3, 18, noble, "[Ne] 3s² 3p⁶", "Khí", "Không màu", "1,784 g/L", "−189,3 °C", "−185,8 °C", "0", "Trơ hóa học trong điều kiện thường.", "Khoảng 0,93% khí quyển.", "#BFC7E9"),
                E(19, "K", "Kali", 39.098, 4, 1, alkali, "[Ar] 4s¹", "Rắn", "Kim loại bạc mềm", "0,862 g/cm³", "63,4 °C", "759 °C", "+1", "Phản ứng rất mạnh với nước; ngọn lửa tím.", "Khoáng sylvit, cacnalit; không tồn tại tự do.", "#BBB7C8"),
                E(20, "Ca", "Canxi", 40.078, 4, 2, earth, "[Ar] 4s²", "Rắn", "Kim loại trắng bạc", "1,55 g/cm³", "842 °C", "1.484 °C", "+2", "Tác dụng với nước tạo Ca(OH)₂ và H₂; cháy cho màu đỏ cam.", "Đá vôi, thạch cao, apatit.", "#C8CDD0"),
                E(21, "Sc", "Scandi", 44.956, 4, 3, transition, "[Ar] 3d¹ 4s²", "Rắn", "Kim loại bạc", "2,99 g/cm³", "1.541 °C", "2.836 °C", "+3", "Bị oxi hóa chậm trong không khí; phản ứng với axit loãng.", "Khoáng hiếm, đi kèm đất hiếm.", "#C7CCCE"),
                E(22, "Ti", "Titan", 47.867, 4, 4, transition, "[Ar] 3d² 4s²", "Rắn", "Kim loại xám bạc", "4,51 g/cm³", "1.668 °C", "3.287 °C", "+2, +3, +4", "Bền ăn mòn nhờ màng TiO₂; hoạt động hơn khi đun nóng.", "Ilmenit và rutin.", "#AEB6BA"),
                E(23, "V", "Vanadi", 50.942, 4, 5, transition, "[Ar] 3d³ 4s²", "Rắn", "Kim loại xám bạc", "6,11 g/cm³", "1.910 °C", "3.407 °C", "+2, +3, +4, +5", "Có nhiều số oxi hóa với màu dung dịch đặc trưng.", "Vanadinit và quặng titan–sắt.", "#9FA7A7"),
                E(24, "Cr", "Crom", 51.996, 4, 6, transition, "[Ar] 3d⁵ 4s¹", "Rắn", "Kim loại bạc bóng", "7,15 g/cm³", "1.907 °C", "2.671 °C", "+2, +3, +6", "Cr(III) tương đối bền; Cr(VI) oxi hóa mạnh và độc.", "Quặng cromit FeCr₂O₄.", "#C5C7C9"),
                E(25, "Mn", "Mangan", 54.938, 4, 7, transition, "[Ar] 3d⁵ 4s²", "Rắn", "Kim loại xám bạc", "7,21 g/cm³", "1.246 °C", "2.061 °C", "+2, +4, +6, +7", "KMnO₄ là chất oxi hóa mạnh; MnO₂ xúc tác phân hủy H₂O₂.", "Piroluzit MnO₂.", "#A9AAAC"),
                E(26, "Fe", "Sắt", 55.845, 4, 8, transition, "[Ar] 3d⁶ 4s²", "Rắn", "Kim loại xám bạc", "7,87 g/cm³", "1.538 °C", "2.862 °C", "+2, +3", "Tác dụng với axit loãng; dễ bị ăn mòn điện hóa trong không khí ẩm.", "Hematit, manhetit và siderit.", "#858A8D"),
                E(27, "Co", "Coban", 58.933, 4, 9, transition, "[Ar] 3d⁷ 4s²", "Rắn", "Kim loại xám xanh", "8,90 g/cm³", "1.495 °C", "2.927 °C", "+2, +3", "Bị oxi hóa khi nung; muối Co(II) thường hồng hoặc xanh tùy phối tử.", "Cobanit và quặng niken–đồng.", "#8C9398"),
                E(28, "Ni", "Niken", 58.693, 4, 10, transition, "[Ar] 3d⁸ 4s²", "Rắn", "Kim loại trắng bạc", "8,91 g/cm³", "1.455 °C", "2.913 °C", "+2, +3", "Bền ăn mòn, có tính xúc tác; muối Ni(II) thường xanh lục.", "Pentlandit và laterit.", "#B9BFC0"),
                E(29, "Cu", "Đồng", 63.546, 4, 11, transition, "[Ar] 3d¹⁰ 4s¹", "Rắn", "Kim loại đỏ cam", "8,96 g/cm³", "1.084,6 °C", "2.562 °C", "+1, +2", "Kém hoạt động hơn H; phản ứng với chất oxi hóa và thế Ag khỏi AgNO₃.", "Cancopirit, malachit và đồng tự sinh.", "#B96843"),
                E(30, "Zn", "Kẽm", 65.38, 4, 12, transition, "[Ar] 3d¹⁰ 4s²", "Rắn", "Kim loại xám xanh", "7,14 g/cm³", "419,5 °C", "907 °C", "+2", "ZnO và Zn(OH)₂ lưỡng tính; Zn đẩy H₂ khỏi axit loãng.", "Quặng sphalerit ZnS.", "#ABB4B5"),
                E(31, "Ga", "Gali", 69.723, 4, 13, post, "[Ar] 3d¹⁰ 4s² 4p¹", "Rắn", "Kim loại bạc mềm", "5,91 g/cm³", "29,8 °C", "2.204 °C", "+3", "Nóng chảy gần nhiệt độ cơ thể; hợp chất Ga(III) chiếm ưu thế.", "Đi kèm quặng bôxit và kẽm.", "#C0C6C8"),
                E(32, "Ge", "Gecmani", 72.630, 4, 14, metalloid, "[Ar] 3d¹⁰ 4s² 4p²", "Rắn", "Á kim xám bạc", "5,32 g/cm³", "938,3 °C", "2.833 °C", "+2, +4", "Bán dẫn; GeO₂ thể hiện tính oxit axit yếu.", "Đi kèm quặng kẽm và than.", "#8D9596"),
                E(33, "As", "Asen", 74.922, 4, 15, metalloid, "[Ar] 3d¹⁰ 4s² 4p³", "Rắn", "Xám thép, giòn", "5,73 g/cm³", "Thăng hoa khoảng 615 °C", "Thăng hoa", "−3, +3, +5", "Nhiều hợp chất rất độc; tạo oxit As₂O₃ và As₂O₅.", "Arsenopyrit FeAsS.", "#6E7472"),
                E(34, "Se", "Selen", 78.971, 4, 16, nonmetal, "[Ar] 3d¹⁰ 4s² 4p⁴", "Rắn", "Xám hoặc đỏ", "4,81 g/cm³", "221 °C", "685 °C", "−2, +4, +6", "Tính chất gần lưu huỳnh; lượng vi lượng cần thiết nhưng dư gây độc.", "Đi kèm quặng sunfua kim loại.", "#6F7371"),
                E(35, "Br", "Brom", 79.904, 4, 17, halogen, "[Ar] 3d¹⁰ 4s² 4p⁵", "Lỏng", "Đỏ nâu, bay hơi mạnh", "3,12 g/cm³", "−7,2 °C", "58,8 °C", "−1, +1, +3, +5, +7", "Chất oxi hóa; kém hoạt động hơn clo, mạnh hơn iot; hơi độc.", "Nước biển và nước muối.", "#7E2F23"),
                E(36, "Kr", "Krypton", 83.798, 4, 18, noble, "[Ar] 3d¹⁰ 4s² 4p⁶", "Khí", "Không màu", "3,75 g/L", "−157,4 °C", "−153,4 °C", "0, hiếm +2", "Rất trơ; tạo một số hợp chất với flo trong điều kiện đặc biệt.", "Lượng vết trong khí quyển.", "#C3C8E3"),
                E(37, "Rb", "Rubidi", 85.468, 5, 1, alkali, "[Kr] 5s¹", "Rắn", "Kim loại bạc mềm", "1,53 g/cm³", "39,3 °C", "688 °C", "+1", "Tự bốc cháy trong không khí ẩm; phản ứng dữ dội với nước.", "Phân tán trong lepidolit và pollucit.", "#B9B1C4"),
                E(38, "Sr", "Stronti", 87.62, 5, 2, earth, "[Kr] 5s²", "Rắn", "Kim loại bạc", "2,64 g/cm³", "777 °C", "1.382 °C", "+2", "Phản ứng với nước; muối tạo màu đỏ thẫm cho ngọn lửa.", "Celestin SrSO₄ và strontianit.", "#C8CDD0"),
                E(47, "Ag", "Bạc", 107.868, 5, 11, transition, "[Kr] 4d¹⁰ 5s¹", "Rắn", "Kim loại trắng sáng", "10,49 g/cm³", "961,8 °C", "2.162 °C", "+1", "Ít hoạt động; Ag⁺ tạo kết tủa với Cl⁻, Br⁻, I⁻.", "Acanthit và quặng chì–kẽm–đồng.", "#D4D8DA"),
                E(48, "Cd", "Cadimi", 112.414, 5, 12, transition, "[Kr] 4d¹⁰ 5s²", "Rắn", "Kim loại trắng xanh", "8,65 g/cm³", "321,1 °C", "767 °C", "+2", "Kim loại và hợp chất độc; CdS có màu vàng.", "Sản phẩm phụ luyện kẽm.", "#AEB7B8"),
                E(50, "Sn", "Thiếc", 118.710, 5, 14, post, "[Kr] 4d¹⁰ 5s² 5p²", "Rắn", "Kim loại trắng bạc", "7,31 g/cm³", "231,9 °C", "2.602 °C", "+2, +4", "Khá bền; SnO₂ lưỡng tính, Sn(II) có tính khử.", "Quặng cassiterit SnO₂.", "#BBC2C3"),
                E(53, "I", "Iot", 126.904, 5, 17, halogen, "[Kr] 4d¹⁰ 5s² 5p⁵", "Rắn", "Tinh thể tím đen", "4,93 g/cm³", "113,7 °C", "184,3 °C", "−1, +1, +5, +7", "Thăng hoa cho hơi tím; oxi hóa yếu hơn brom; tạo phức xanh với hồ tinh bột.", "Iodua trong nước biển và nước muối.", "#3D274E"),
                E(54, "Xe", "Xenon", 131.293, 5, 18, noble, "[Kr] 4d¹⁰ 5s² 5p⁶", "Khí", "Không màu", "5,89 g/L", "−111,8 °C", "−108,1 °C", "0, +2, +4, +6, +8", "Khí hiếm nhưng có thể tạo florua và oxit.", "Lượng vết rất nhỏ trong khí quyển.", "#BFC4DE"),
                E(55, "Cs", "Xesi", 132.905, 6, 1, alkali, "[Xe] 6s¹", "Rắn", "Kim loại vàng nhạt", "1,93 g/cm³", "28,4 °C", "671 °C", "+1", "Một trong các kim loại hoạt động nhất; nổ khi gặp nước.", "Khoáng pollucit.", "#C8B891"),
                E(56, "Ba", "Bari", 137.327, 6, 2, earth, "[Xe] 6s²", "Rắn", "Kim loại trắng bạc", "3,62 g/cm³", "727 °C", "1.897 °C", "+2", "Phản ứng với nước; Ba²⁺ tạo BaSO₄ trắng rất ít tan; muối tan độc.", "Barit BaSO₄ và witherit BaCO₃.", "#C5CBCD"),
                E(74, "W", "Vonfram", 183.84, 6, 6, transition, "[Xe] 4f¹⁴ 5d⁴ 6s²", "Rắn", "Kim loại xám thép", "19,25 g/cm³", "3.422 °C", "5.555 °C", "+4, +6", "Rất bền nhiệt; WO₃ là oxit axit, tạo vonframat.", "Wolframit và scheelit.", "#6F7476"),
                E(78, "Pt", "Platin", 195.084, 6, 10, transition, "[Xe] 4f¹⁴ 5d⁹ 6s¹", "Rắn", "Kim loại trắng xám", "21,45 g/cm³", "1.768 °C", "3.825 °C", "+2, +4", "Kim loại quý, bền hóa học và xúc tác tốt; tan trong nước cường toan.", "Quặng sunfua Ni–Cu và sa khoáng.", "#C8CBCD"),
                E(79, "Au", "Vàng", 196.967, 6, 11, transition, "[Xe] 4f¹⁴ 5d¹⁰ 6s¹", "Rắn", "Kim loại vàng", "19,32 g/cm³", "1.064,2 °C", "2.856 °C", "+1, +3", "Rất kém hoạt động; tan trong nước cường toan và dung dịch xianua có O₂.", "Vàng tự sinh và quặng sunfua.", "#D5A52F"),
                E(80, "Hg", "Thủy ngân", 200.592, 6, 12, transition, "[Xe] 4f¹⁴ 5d¹⁰ 6s²", "Lỏng", "Kim loại bạc, tạo giọt", "13,53 g/cm³", "−38,8 °C", "356,7 °C", "+1, +2", "Tạo hỗn hống với nhiều kim loại; hơi và hợp chất rất độc.", "Quặng cinnabar HgS.", "#BFC5C6"),
                E(82, "Pb", "Chì", 207.2, 6, 14, post, "[Xe] 4f¹⁴ 5d¹⁰ 6s² 6p²", "Rắn", "Kim loại xám xanh, mềm", "11,34 g/cm³", "327,5 °C", "1.749 °C", "+2, +4", "Bị thụ động bởi lớp muối ít tan; hợp chất tích lũy sinh học và độc.", "Galen PbS.", "#6C7376"),
                E(88, "Ra", "Radi", 226, 7, 2, earth, "[Rn] 7s²", "Rắn", "Kim loại trắng bạc, phóng xạ", "Khoảng 5,5 g/cm³", "Khoảng 700 °C", "Khoảng 1.737 °C", "+2", "Phóng xạ mạnh; tính hóa học gần bari, không dùng trong thí nghiệm phổ thông.", "Vết trong quặng urani.", "#B9C1B8"),
                E(92, "U", "Urani", 238.029, 7, 0, ElementCategory.Actinide, "[Rn] 5f³ 6d¹ 7s²", "Rắn", "Kim loại xám bạc, phóng xạ", "19,05 g/cm³", "1.132 °C", "4.131 °C", "+3, +4, +5, +6", "Dễ oxi hóa; hợp chất uranyl U(VI) thường vàng; có độc tính và phóng xạ.", "Uraninit và quặng photphat.", "#7B806E")
            };
        }

        private static Color Hex(string value)
        {
            Color colour;
            return ColorUtility.TryParseHtmlString(value, out colour) ? colour : Color.magenta;
        }
    }
}
