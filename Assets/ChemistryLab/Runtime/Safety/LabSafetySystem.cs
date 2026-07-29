using System;
using System.Collections.Generic;

namespace ChemistryLab.Desktop
{
    public sealed class ChemicalHazardAssessment
    {
        public HazardSeverity Severity;
        public string Message;
    }

    public static class ChemicalHazardClassifier
    {
        public static ChemicalHazardAssessment Classify(ChemicalDefinition chemical)
        {
            if (chemical == null || string.IsNullOrWhiteSpace(chemical.Hazards))
            {
                return new ChemicalHazardAssessment
                {
                    Severity = HazardSeverity.None,
                    Message = "Chưa có dữ liệu nguy hại."
                };
            }

            var text = chemical.Hazards.ToLowerInvariant();
            var severity = text.Contains("cực độc")
                || text.Contains("nguy hại sinh sản")
                ? HazardSeverity.Critical
                : text.Contains("độc")
                  || text.Contains("ăn mòn mạnh")
                  || text.Contains("gây bỏng")
                  || text.Contains("chất oxi hóa")
                    ? HazardSeverity.Dangerous
                    : text.Contains("có hại")
                      || text.Contains("kích ứng")
                      || text.Contains("dễ cháy")
                        ? HazardSeverity.Caution
                        : HazardSeverity.None;
            return new ChemicalHazardAssessment
            {
                Severity = severity,
                Message = chemical.Formula + " · " + chemical.Hazards + " " + chemical.Handling
            };
        }
    }

    public enum HazardSeverity
    {
        None,
        Caution,
        Dangerous,
        Critical
    }

    public enum AirborneHazardKind
    {
        None,
        Asphyxiant,
        Flammable,
        Oxidising,
        Toxic,
        CorrosiveToxic
    }

    public sealed class AirborneHazard
    {
        public AirborneHazard(
            string formula,
            string name,
            AirborneHazardKind kind,
            HazardSeverity severity,
            float respiratorEfficiency,
            string warning)
        {
            Formula = formula;
            Name = name;
            Kind = kind;
            Severity = severity;
            RespiratorEfficiency = respiratorEfficiency;
            Warning = warning;
        }

        public string Formula { get; private set; }
        public string Name { get; private set; }
        public AirborneHazardKind Kind { get; private set; }
        public HazardSeverity Severity { get; private set; }
        public float RespiratorEfficiency { get; private set; }
        public string Warning { get; private set; }

        public bool IsHazardous
        {
            get { return Severity != HazardSeverity.None; }
        }
    }

    public static class AirborneHazardCatalog
    {
        private static readonly Dictionary<string, AirborneHazard> ByFormula = Build();

        public static AirborneHazard Find(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                return null;
            }

            AirborneHazard hazard;
            return ByFormula.TryGetValue(Normalise(formula), out hazard) ? hazard : null;
        }

        public static void ValidateOrThrow()
        {
            var required = new[] { "H2S", "NH3", "CO2", "H2", "O2", "CL2", "NO", "NO2", "SO2" };
            for (var index = 0; index < required.Length; index++)
            {
                if (!ByFormula.ContainsKey(required[index]))
                {
                    throw new InvalidOperationException(
                        "Missing airborne hazard profile: " + required[index]);
                }
            }
        }

        private static Dictionary<string, AirborneHazard> Build()
        {
            return new Dictionary<string, AirborneHazard>(StringComparer.Ordinal)
            {
                {
                    "CO2",
                    new AirborneHazard(
                        "CO₂", "Cacbon đioxit", AirborneHazardKind.Asphyxiant,
                        HazardSeverity.Caution, .20f,
                        "CO₂ có thể chiếm chỗ oxy trong không gian kín.")
                },
                {
                    "H2",
                    new AirborneHazard(
                        "H₂", "Hiđro", AirborneHazardKind.Flammable,
                        HazardSeverity.Dangerous, 0f,
                        "H₂ tạo hỗn hợp dễ cháy nổ với không khí; mặt nạ không ngăn nguy cơ bắt lửa.")
                },
                {
                    "O2",
                    new AirborneHazard(
                        "O₂", "Oxi", AirborneHazardKind.Oxidising,
                        HazardSeverity.Caution, 0f,
                        "O₂ làm tăng tốc độ cháy; tránh dầu mỡ, tia lửa và chất dễ cháy.")
                },
                {
                    "NH3",
                    new AirborneHazard(
                        "NH₃", "Amoniac", AirborneHazardKind.CorrosiveToxic,
                        HazardSeverity.Dangerous, .82f,
                        "NH₃ gây bỏng mắt, da và đường hô hấp; không kiểm tra bằng cách ngửi.")
                },
                {
                    "H2S",
                    new AirborneHazard(
                        "H₂S", "Hiđro sunfua", AirborneHazardKind.Toxic,
                        HazardSeverity.Critical, .88f,
                        "H₂S cực độc và có thể làm mất khả năng nhận biết mùi rất nhanh.")
                },
                {
                    "CL2",
                    new AirborneHazard(
                        "Cl₂", "Clo", AirborneHazardKind.CorrosiveToxic,
                        HazardSeverity.Critical, .90f,
                        "Cl₂ độc, ăn mòn và gây tổn thương phổi.")
                },
                {
                    "NO",
                    new AirborneHazard(
                        "NO", "Nitơ monoxit", AirborneHazardKind.Toxic,
                        HazardSeverity.Dangerous, .82f,
                        "NO độc và nhanh chóng tạo NO₂ độc màu nâu khi tiếp xúc với không khí.")
                },
                {
                    "NO2",
                    new AirborneHazard(
                        "NO₂", "Nitơ đioxit", AirborneHazardKind.CorrosiveToxic,
                        HazardSeverity.Critical, .86f,
                        "NO₂ độc và có thể gây phù phổi muộn.")
                },
                {
                    "SO2",
                    new AirborneHazard(
                        "SO₂", "Lưu huỳnh đioxit", AirborneHazardKind.CorrosiveToxic,
                        HazardSeverity.Dangerous, .84f,
                        "SO₂ kích ứng mạnh mắt và đường hô hấp.")
                }
            };
        }

        private static string Normalise(string formula)
        {
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
                .Replace(" ", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }
    }

    public sealed class SafetyIncident
    {
        public HazardSeverity Severity;
        public string Title;
        public string Message;
        public float ExposureFraction;
        public float HealthLost;
        public int CreditsLost;
        public bool EmergencyEvacuation;
        public bool Controlled;
    }

    /// <summary>
    /// Persistent run-state for the avatar's health, credits and protective equipment.
    /// Values are intentionally game-scale consequences, not medical dose guidance.
    /// </summary>
    public sealed class LabSafetySystem
    {
        public const int RespiratorPrice = 250;
        public const int StartingCredits = 1200;

        public LabSafetySystem()
        {
            Health = 100f;
            Credits = StartingCredits;
            LastIncident = new SafetyIncident
            {
                Severity = HazardSeverity.None,
                Title = "Ca trực an toàn",
                Message = "Chưa ghi nhận phơi nhiễm.",
                Controlled = true
            };
        }

        public float Health { get; private set; }
        public int Credits { get; private set; }
        public bool RespiratorOwned { get; private set; }
        public bool RespiratorEquipped { get; private set; }
        public bool GasTrapConnected { get; private set; }
        public int IncidentCount { get; private set; }
        public float TotalExposure { get; private set; }
        public SafetyIncident LastIncident { get; private set; }

        public string BuyOrToggleRespirator()
        {
            if (!RespiratorOwned)
            {
                if (Credits < RespiratorPrice)
                {
                    return "Không đủ tín dụng để mua mặt nạ lọc độc.";
                }

                Credits -= RespiratorPrice;
                RespiratorOwned = true;
                RespiratorEquipped = true;
                return "Đã mua và đeo mặt nạ lọc độc. Bộ lọc không thay thế tủ hút.";
            }

            RespiratorEquipped = !RespiratorEquipped;
            return RespiratorEquipped
                ? "Đã đeo mặt nạ lọc độc."
                : "Đã tháo mặt nạ lọc độc.";
        }

        public string ToggleGasTrap()
        {
            GasTrapConnected = !GasTrapConnected;
            return GasTrapConnected
                ? "Đã nối bình cách ly khí vào cốc trong tủ hút."
                : "Đã tháo bình cách ly khí.";
        }

        public SafetyIncident Apply(ReactionOutcome outcome, LabStation station)
        {
            if (outcome == null || outcome.Hazard == null || !outcome.Hazard.IsHazardous)
            {
                LastIncident = new SafetyIncident
                {
                    Severity = HazardSeverity.None,
                    Title = "Không phát tán độc chất",
                    Message = "Phản ứng không tạo nguy cơ khí đáng kể trong mô hình hiện tại.",
                    Controlled = true
                };
                return LastIncident;
            }

            var hoodCapture = station == LabStation.FumeHood ? .90f : 0f;
            if (station == LabStation.FumeHood && GasTrapConnected)
            {
                hoodCapture = .995f;
            }

            var respiratorProtection = RespiratorEquipped
                ? outcome.Hazard.RespiratorEfficiency
                : 0f;
            var exposure = (1f - hoodCapture) * (1f - respiratorProtection);
            var quantityScale = Clamp(
                .55f + (float)Math.Log10(1d + Math.Max(.01d, outcome.ReleasedGasGrams) * 4d),
                .55f,
                1.6f);
            var baseDamage = BaseDamage(outcome.Hazard.Severity);
            var healthLost = baseDamage * exposure * quantityScale;
            var controlled = exposure <= .02f;
            var creditsLost = controlled
                ? 0
                : (int)Math.Ceiling(healthLost * 7f + (int)outcome.Hazard.Severity * 18f);

            Health = Clamp(Health - healthLost, 0f, 100f);
            Credits = Math.Max(0, Credits - creditsLost);
            TotalExposure += exposure * quantityScale;
            if (!controlled)
            {
                IncidentCount++;
            }

            var emergency = Health <= 0.01f;
            if (emergency)
            {
                var evacuationCost = Math.Min(Credits, 300);
                Credits -= evacuationCost;
                creditsLost += evacuationCost;
                Health = 35f;
            }

            LastIncident = new SafetyIncident
            {
                Severity = outcome.Hazard.Severity,
                Title = controlled
                    ? "Khí đã được kiểm soát"
                    : emergency ? "Bất tỉnh · sơ tán khẩn cấp" : "Phơi nhiễm " + outcome.Hazard.Formula,
                Message = BuildIncidentMessage(
                    outcome,
                    station,
                    exposure,
                    healthLost,
                    creditsLost,
                    emergency,
                    controlled),
                ExposureFraction = exposure,
                HealthLost = healthLost,
                CreditsLost = creditsLost,
                EmergencyEvacuation = emergency,
                Controlled = controlled
            };
            return LastIncident;
        }

        public static void ValidateOrThrow()
        {
            var leadWarning = ChemicalHazardClassifier.Classify(
                DesktopChemistryDatabase.GetChemical("lead-nitrate"));
            if (leadWarning.Severity != HazardSeverity.Critical)
            {
                throw new InvalidOperationException("Toxic chemical warning validation failed.");
            }

            var unsafeSystem = new LabSafetySystem();
            var protectedSystem = new LabSafetySystem();
            protectedSystem.ToggleGasTrap();
            var outcome = new ReactionOutcome
            {
                Status = ReactionStatus.Reaction,
                Hazard = AirborneHazardCatalog.Find("H₂S"),
                ReleasedGasGrams = 2d
            };
            var unsafeIncident = unsafeSystem.Apply(outcome, LabStation.Workbench);
            var protectedIncident = protectedSystem.Apply(outcome, LabStation.FumeHood);
            if (unsafeIncident.HealthLost <= 20f
                || unsafeIncident.CreditsLost <= 0
                || protectedIncident.HealthLost >= unsafeIncident.HealthLost * .02f
                || !protectedIncident.Controlled)
            {
                throw new InvalidOperationException("Lab safety consequence validation failed.");
            }
        }

        private static string BuildIncidentMessage(
            ReactionOutcome outcome,
            LabStation station,
            float exposure,
            float healthLost,
            int creditsLost,
            bool emergency,
            bool controlled)
        {
            if (controlled)
            {
                return outcome.Hazard.Formula + " đã được giữ trong hệ cách ly; liều tồn dư "
                    + (exposure * 100f).ToString("0.0") + "%.";
            }

            var controlNote = station == LabStation.FumeHood
                ? "Tủ hút đã giảm phát tán nhưng cấu hình bảo vệ chưa đủ."
                : "Phản ứng diễn ra ngoài tủ hút.";
            return controlNote + " " + outcome.Hazard.Warning
                + " Nhân vật mất " + healthLost.ToString("0.0") + " sức khỏe"
                + " và trả " + creditsLost + " tín dụng"
                + (emergency ? " cho sơ tán, cấp cứu và khử nhiễm." : " cho điều trị/khử nhiễm.");
        }

        private static float BaseDamage(HazardSeverity severity)
        {
            switch (severity)
            {
                case HazardSeverity.Critical: return 55f;
                case HazardSeverity.Dangerous: return 25f;
                case HazardSeverity.Caution: return 8f;
                default: return 0f;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
